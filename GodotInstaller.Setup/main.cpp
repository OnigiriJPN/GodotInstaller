#include <iostream>
#include <string>
#include <thread>
#include <chrono>
#include <atomic>
#include <vector>
#include <conio.h>
#include <windows.h>
#include <shlobj.h>
#include <tchar.h>
#include <urlmon.h>

// C++/WinRT headers
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Xml.Dom.h>
#include <winrt/Windows.UI.Notifications.h>

#pragma comment(lib, "urlmon.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "windowsapp.lib")

using namespace winrt;
using namespace Windows::Data::Xml::Dom;
using namespace Windows::UI::Notifications;

// --- ユーティリティ: タイプライター風出力 ---
void TypePrint(const std::wstring& text, int delayMs = 30) {
    for (wchar_t c : text) {
        std::wcout << c;
        std::wcout.flush();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
    }
}

// --- 画面クリア関数 ---
void ClearScreen() {
    system("cls");
}

// --- .NET 10 レジストリ検出ロジック ---
bool CheckNet10DesktopRuntime() {
    HKEY hKey;
    LONG result = RegOpenKeyEx(
        HKEY_LOCAL_MACHINE,
        _T("SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x64\\sharedfx\\Microsoft.WindowsDesktop.App"),
        0,
        KEY_READ | KEY_WOW64_64KEY,
        &hKey
    );

    if (result != ERROR_SUCCESS) return false;

    DWORD index = 0;
    TCHAR valueName[256];
    DWORD valueNameSize = 256;
    bool found = false;

    while (true) {
        valueNameSize = 256;
        LONG enumResult = RegEnumValue(hKey, index, valueName, &valueNameSize, NULL, NULL, NULL, NULL);
        if (enumResult != ERROR_SUCCESS) break;

        std::wstring verStr(valueName);
        if (verStr.find(L"10.0") != std::wstring::npos) {
            found = true;
            break;
        }
        index++;
    }

    RegCloseKey(hKey);
    return found;
}

// --- スピナーアニメーション付きチェック ---
bool CheckWithSpinner() {
    std::atomic<bool> isChecking(true);
    bool result = false;

    std::thread checkThread([&]() {
        std::this_thread::sleep_for(std::chrono::milliseconds(1200)); // 演出用ウェイト
        result = CheckNet10DesktopRuntime();
        isChecking = false;
        });

    const wchar_t spinnerChars[] = { L'|', L'/', L'-', L'\\' };
    int i = 0;
    std::wcout << L"GodotInstallerを準備しています ";
    while (isChecking) {
        std::wcout << L"\b" << spinnerChars[i];
        std::wcout.flush();
        i = (i + 1) % 4;
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }

    checkThread.join();
    std::wcout << L"\r                                                \r";
    return result;
}

// --- フォルダ選択ダイアログ (Windows API) ---
std::wstring BrowseFolder() {
    std::wstring folderPath = L"";
    BROWSEINFO bi = { 0 };
    bi.lpszTitle = _T(".NET 10 セットアップexeがあるフォルダを選択してください");
    bi.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;

    LPITEMIDLIST pidl = SHBrowseForFolder(&bi);
    if (pidl != NULL) {
        TCHAR path[MAX_PATH];
        if (SHGetPathFromIDList(pidl, path)) {
            folderPath = path;
        }
        CoTaskMemFree(pidl);
    }
    return folderPath;
}

// --- プロセス実行ヘルパー ---
bool RunExecutable(const std::wstring& exePath, const std::wstring& args = L"") {
    std::wstring cmd = exePath + (args.empty() ? L"" : L" " + args);
    STARTUPINFO si = { sizeof(STARTUPINFO) };
    PROCESS_INFORMATION pi;

    if (CreateProcess(NULL, (LPWSTR)cmd.c_str(), NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) {
        WaitForSingleObject(pi.hProcess, INFINITE);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        return true;
    }
    return false;
}

// --- 矢印キー対応インタラクティブメニュー ---
int ShowInteractiveMenu(const std::vector<std::wstring>& options) {
    int selected = 0;
    int key = 0;

    for (size_t i = 0; i < options.size(); ++i) {
        if (i == selected) std::wcout << L" > " << options[i] << L"\n";
        else std::wcout << L"   " << options[i] << L"\n";
    }

    while (true) {
        key = _getch();
        if (key == 224) {
            key = _getch();
            if (key == 72) { // 上
                selected = (selected - 1 + (int)options.size()) % (int)options.size();
            }
            else if (key == 80) { // 下
                selected = (selected + 1) % (int)options.size();
            }
        }
        else if (key == 13) { // Enter
            return selected;
        }

        // カーソルをメニューの先頭に戻して再描画
        std::wcout << "\033[" << options.size() << "A";
        for (size_t i = 0; i < options.size(); ++i) {
            if (i == selected) std::wcout << L" > " << options[i] << L"   \n";
            else std::wcout << L"   " << options[i] << L"   \n";
        }
    }
}

// --- WinRT トースト通知の送信 ---
void ShowToastNotification() {
    try {
        std::wstring xmlString =
            L"<toast>"
            L"  <visual>"
            L"    <binding template=\"ToastGeneric\">"
            L"      <text>GodotInstaller セットアップ</text>"
            L"      <text>.NET 10 Desktop Runtimeをインストールしました。</text>"
            L"    </binding>"
            L"  </visual>"
            L"</toast>";

        XmlDocument doc;
        doc.LoadXml(xmlString);

        ToastNotification toast(doc);
        auto notifier = ToastNotificationManager::CreateToastNotifier();
        notifier.Show(toast);
    }
    catch (...) {
        // 通知に失敗した場合もコンソール側の処理は継続
    }
}

int main() {
    // コンソールの文字コードをUTF-8に設定
    SetConsoleOutputCP(CP_UTF8);
    std::wcout.imbue(std::locale(""));

    // WinRT初期化
    init_apartment();

    // 1. ようこそメッセージ
    TypePrint(L"GodotInstallerへようこそ！\n\n", 30);

    // 2. 準備中アニメーション ＆ .NET 10 チェック
    bool hasDotNet = CheckWithSpinner();

    if (hasDotNet) {
        std::wcout << L"[OK] .NET 10 Desktop Runtime が検出されました。\n";
        std::wcout << L"メインインストーラーを起動します...\n";
        RunExecutable(L"GodotInstaller.exe");
        return 0;
    }

    // 3. 未インストールの処理：画面クリア ＆ メニュー選択
    ClearScreen();
    TypePrint(L".NET 10 Desktop Runtime がありません。\nインストールしますか？\n\n", 25);

    std::vector<std::wstring> options = {
        L"ダウンロードしながらインストール",
        L"手動でインストールする (フォルダ指定)",
        L"全て手動でランタイムを入れる (ブラウザを開く)",
        L"キャンセル"
    };

    int choice = ShowInteractiveMenu(options);
    std::wcout << L"\n\n";

    switch (choice) {
    case 0: { // 自動ダウンロード＋インストール
        std::wcout << L"[+] .NET 10 インストーラーをダウンロードしています...\n";
        std::wstring installerUrl = L"https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.exe";
        std::wstring savePath = L"dotnet_installer.exe";

        HRESULT hr = URLDownloadToFile(NULL, installerUrl.c_str(), savePath.c_str(), 0, NULL);
        if (hr == S_OK) {
            std::wcout << L"[+] ダウンロード完了。インストールを実行中...\n";
            RunExecutable(savePath, L"/install /quiet /norestart");

            // インストール完了通知
            ShowToastNotification();

            std::wcout << L"[+] GodotInstallerを起動します。\n";
            RunExecutable(L"GodotInstaller.exe");
        }
        else {
            std::wcout << L"[エラー] ダウンロードに失敗しました。\n";
        }
        break;
    }
    case 1: { // フォルダ指定の手動インストール
        std::wcout << L"[+] セットアップexeが含まれるフォルダを選択してください...\n";
        std::wstring folder = BrowseFolder();
        if (!folder.empty()) {
            std::wstring targetExe = folder + L"\\windowsdesktop-runtime-10.0-win-x64.exe";
            std::wcout << L"[+] 実行中: " << targetExe << L"\n";
            if (RunExecutable(targetExe)) {
                ShowToastNotification();
                std::wcout << L"[+] セットアップが完了しました。GodotInstallerを起動します。\n";
                RunExecutable(L"GodotInstaller.exe");
            }
            else {
                std::wcout << L"[エラー] 指定されたファイルを実行できませんでした。\n";
            }
        }
        else {
            std::wcout << L"フォルダの選択がキャンセルされました。\n";
        }
        break;
    }
    case 2: { // ブラウザ誘導
        std::wcout << L"[+] 公式ダウンロードページを開いています...\n";
        ShellExecute(NULL, L"open", L"https://dotnet.microsoft.com/download/dotnet/10.0", NULL, NULL, SW_SHOWNORMAL);
        std::wcout << L"ブラウザからインストールを完了させてください。\n";
        break;
    }
    case 3:
    default:
        std::wcout << L"セットアップを中止します。\n";
        break;
    }

    std::wcout << L"\n終了するには何かキーを押してください...";
    _getch();
    return 0;
}