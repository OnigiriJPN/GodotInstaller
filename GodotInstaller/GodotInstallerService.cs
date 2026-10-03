using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace GodotInstaller
{
    public class GodotInstallerService
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5), // タイムアウトを5分に設定（大容量ダウンロード対策）
            DefaultRequestHeaders = { { "User-Agent", "GodotInstaller-WPF" } }
        };

        /// <summary>
        /// ディスクの空き容量をチェックする
        /// </summary>
        public bool CheckDiskSpace(string installPath, long requiredMegabytes = 500)
        {
            try
            {
                string rootDrive = Path.GetPathRoot(Path.GetFullPath(installPath)) ?? "C:\\";
                DriveInfo drive = new DriveInfo(rootDrive);
                long requiredBytes = requiredMegabytes * 1024 * 1024;

                return drive.AvailableFreeSpace >= requiredBytes;
            }
            catch (Exception ex)
            {
                throw new IOException($"ドライブ情報の取得に失敗しました ({installPath}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// GitHubから最新のGodotダウンロードURLを取得する（エラー処理強化版）
        /// </summary>
        public async Task<string> GetLatestDownloadUrlAsync(Action<string> onLogMessage)
        {
            string apiUrl = "https://api.github.com/repos/godotengine/godot/releases/latest";
            string json;

            try
            {
                onLogMessage("GitHub APIへリクエストを送信しています...");
                json = await httpClient.GetStringAsync(apiUrl);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"GitHubとの通信に失敗しました。インターネット接続を確認してください。(詳細: {ex.Message})", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("GitHubからの応答がタイムアウトしました。しばらくしてから再度お試しください。", ex);
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("assets", out var assets))
                {
                    throw new FormatException("GitHubのレスポンス形式が予期せぬ構造です ('assets' プロパティが見つかりません)。");
                }

                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Contains("win64.zip") || (name.Contains("win") && name.EndsWith(".zip")))
                    {
                        return asset.GetProperty("browser_download_url").GetString()
                               ?? throw new Exception("アセットのダウンロードURLが空です。");
                    }
                }

                throw new Exception("リリース一覧の中にWindows 64bit用のZIPファイルが見つかりませんでした。");
            }
            catch (JsonException ex)
            {
                throw new Exception($"GitHubからのJSONデータの解析に失敗しました。(詳細: {ex.Message})", ex);
            }
        }

        /// <summary>
        /// インストール実行メイン処理（万全のエラーガード付き）
        /// </summary>
        public async Task ExecuteInstallAsync(string installPath, Action<double> onProgressChanged, Action<string> onLogMessage)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"godot_temp_{Guid.NewGuid()}.zip");

            try
            {
                // 1. 空き容量チェック
                onLogMessage($"インストール先ドライブを確認しています: {installPath}");
                if (!CheckDiskSpace(installPath, 500))
                {
                    throw new IOException("インストール先のディスク空き容量が不足しています (最低 500MB 必要です)。");
                }
                onLogMessage("[OK] ディスク空き容量の確認完了。");
                onProgressChanged(10);

                // 2. URL取得
                string zipUrl = await GetLatestDownloadUrlAsync(onLogMessage);
                onLogMessage($"[OK] ダウンロードURLを取得しました。");
                onProgressChanged(30);

                // 3. ダウンロード
                onLogMessage("ZIPファイルのダウンロードを開始します...");

                using (var response = await httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    // HTTPステータスコードが 200番台以外の場合（404や500など）に例外を投げる
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;

                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            double progress = 30 + ((double)totalRead / totalBytes.Value * 40);
                            onProgressChanged(progress);
                        }
                    }
                }
                onLogMessage("[OK] ダウンロードが完了しました。");
                onProgressChanged(70);

                // 4. フォルダ作成・権限確認
                onLogMessage($"インストール先フォルダを確認・作成しています: {installPath}");
                try
                {
                    if (!Directory.Exists(installPath))
                    {
                        Directory.CreateDirectory(installPath);
                    }
                }
                catch(Exception ex) when(ex is UnauthorizedAccessException || ex is IOException)
                {
                    MessageBox.Show($"指定されたフォルダへの書き込み権限がないか、アクセスが拒否されました ({installPath})。管理者権限で実行するか、パスを変更してください。");
                }

                // 5. 展開
                onLogMessage("ファイルを展開しています（これには数秒かかる場合があります）...");
                await Task.Run(() =>
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(tempZipPath, installPath, true);
                    }
                    catch (InvalidDataException ex)
                    {
                        MessageBox.Show($"ダウンロードしたZIPファイルが破損しているか、無効な形式です。 \n ( {ex.Message} )");
                    }
                });

                onLogMessage("[OK] ファイルの展開が正常に完了しました。");
                onProgressChanged(100);
            }
            catch (Exception ex)
            {
                // 内部で発生したエラーにコンテキストを添えて上位（UI）へ再スロー
                onLogMessage($"[エラー] {ex.Message}");
                throw;
            }
            finally
            {
                // 成功しても失敗しても、一時ファイルが残っていれば必ずお掃除する（リソースリーク防止）
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"エラー: {ex.Message} \n {ex.GetType().FullName} \n {ex.HResult:X8}");
                }
            }
        }
    }
}
