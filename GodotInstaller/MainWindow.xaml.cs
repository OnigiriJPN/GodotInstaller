using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace GodotInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings settings;
        private readonly GodotInstallerService installerService = new GodotInstallerService();
        private bool isDarkMode = true;
        private bool isNativeFrame = false;
        public MainWindow()
        {
            InitializeComponent();
            // 1. 起動時に設定（JSON）を読み込む
            settings = AppSettings.Load();
            ApplySettingsToUI();

            // ウィンドウ終了時に設定を自動保存
            this.Closed += (s, e) => settings.Save();
        }
        // hWnd作成&ロード後
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowThemeHelper.SetWindowTheme(this, isDarkMode); // 💡 ヘルパーを利用
            // Windows 11以降のモダンで洗練されている上品なマイカ効果を有効にする
            MicaManager.EnableMicaIfSupported(this);
        }
        // 読み込んだ設定をUIやウィンドウの状態に反映
        private void ApplySettingsToUI()
        {
            // テーマの適用
            isDarkMode = settings.IsDarkMode;
            ApplyThemeColors();

            // ネイティブフレーム状態の適用
            isNativeFrame = settings.IsNativeFrame;
            UpdateFrameState();
        }

        // 🔲 ネイティブタイトルバー ⇄ カスタムタイトルバーの切り替え
        private void BtnToggleNativeFrame_Click(object sender, RoutedEventArgs e)
        {
            isNativeFrame = !isNativeFrame;
            settings.IsNativeFrame = isNativeFrame; // 設定に保存
            settings.Save(); // 即座にJSONへ書き出し

            UpdateFrameState();
        }

        // フレーム状態の切り替え実処理
        private void UpdateFrameState()
        {
            if (isNativeFrame)
            {
                WindowChrome.SetWindowChrome(this, null);
                CustomTitleBar.Visibility = Visibility.Visible;
                MinimizeBtn.Visibility = Visibility.Collapsed;
                CloseBtn.Visibility = Visibility.Collapsed;
                TitleAndIcons.Visibility = Visibility.Collapsed;
                ((Grid)CustomTitleBar.Parent).RowDefinitions[0].Height = new GridLength(32);
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                SysMenu.MouseLeftButtonDown -= SysMenu_MouseLeftButtonDown;
            }
            else
            {
                WindowChrome.SetWindowChrome(this, CustomWindowChrome);
                ((Grid)CustomTitleBar.Parent).RowDefinitions[0].Height = new GridLength(32);
                CustomTitleBar.Visibility = Visibility.Visible;
                MinimizeBtn.Visibility = Visibility.Visible;
                CloseBtn.Visibility = Visibility.Visible;
                TitleAndIcons.Visibility = Visibility.Visible;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                SysMenu.MouseLeftButtonDown += SysMenu_MouseLeftButtonDown;

            }
        }

        private void SysMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double screenX = this.Left + 12;
            double screenY = this.Top + 28;

            if (e.ClickCount == 2)
            {
                SystemCommands.CloseWindow(this);
            }
            else if (e.ClickCount == 1)
            {
                SystemCommands.ShowSystemMenu(this, new Point(screenX, screenY));
            }
        }

        // 最小化（SystemCommandsを使用）
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // 閉じる（SystemCommandsを使用）
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // 🌓 テーマ切り替え（ダーク ⇄ ライト）
        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;
            settings.IsDarkMode = isDarkMode; // 設定に保存
            settings.Save(); // 即座にJSONへ書き出し

            ApplyThemeColors();
        }

        // カラーリソースの切り替え
        private void ApplyThemeColors()
        {
            var dict = Application.Current.Resources;

            if (isDarkMode)
            {
                dict["AppBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
                dict["AppPanelBrush"] = new SolidColorBrush(Color.FromArgb(255, 43, 43, 43));
                dict["AppTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 223, 223, 223));
                dict["AppBorderBrush"] = new SolidColorBrush(Color.FromArgb(255, 63, 63, 63));
                dict["TitleBarBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 31, 31, 31));
                dict["TitleBarTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 204, 204, 204));
                dict["Win11HoverBrush"] = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255));
                dict["Win11PressBrush"] = new SolidColorBrush(Color.FromArgb(85, 255, 255, 255));

                // ダーク時のツールチップ
                dict["ToolTipBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 43, 43, 43));
                dict["ToolTipTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 223, 223, 223));
            }
            else
            {
                dict["AppBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 243, 243, 243));
                dict["AppPanelBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

                // 💡 ライト時の文字色をしっかり濃い色（黒に近い色）にする
                dict["AppTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 20, 20, 20));

                dict["AppBorderBrush"] = new SolidColorBrush(Color.FromArgb(255, 213, 213, 213));
                dict["TitleBarBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 238, 238, 238));

                // 💡 タイトルバーの文字色も濃くする
                dict["TitleBarTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));

                dict["Win11HoverBrush"] = new SolidColorBrush(Color.FromArgb(26, 0, 0, 0));
                dict["Win11PressBrush"] = new SolidColorBrush(Color.FromArgb(51, 0, 0, 0));

                // ツールチップ
                dict["ToolTipBgBrush"] = new SolidColorBrush(Color.FromArgb(255, 255, 255, 225)); // 伝統の黄色[cite: 1]
                dict["ToolTipTextBrush"] = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            }

            // ヘルパー経由でDWMテーマを更新
            WindowThemeHelper.SetWindowTheme(this, isDarkMode);
            this.InvalidateVisual();
        }

        // EULA同意チェックボックス変更
        private void ChkAgree_Changed(object sender, RoutedEventArgs e)
        {
            BtnNext.IsEnabled = ChkAgree.IsChecked ?? false;
        }

        // EULA画面 → インストール画面
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            GridEula.Visibility = Visibility.Collapsed;
            GridInstall.Visibility = Visibility.Visible;
        }

        // インストール画面 → EULA画面
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            GridInstall.Visibility = Visibility.Collapsed;
            GridEula.Visibility = Visibility.Visible;
        }

        // 詳細ログのトグル表示
        private void BtnToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            if (TxtLog.Visibility == Visibility.Visible)
            {
                TxtLog.Visibility = Visibility.Collapsed;
                BtnToggleDetails.Content = "詳細を表示 ▼";
            }
            else
            {
                TxtLog.Visibility = Visibility.Visible;
                BtnToggleDetails.Content = "詳細を隠す ▲";
            }
        }

        // 💡 サービス層を呼び出すだけですっきりしたインストール実行ボタン
        private async void BtnStartInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnStartInstall.IsEnabled = false;
            ProgressBarMain.Value = 0;
            TxtLog.Clear();

            try
            {
                TxtStatus.Text = "インストール準備中...";

                await installerService!.ExecuteInstallAsync(
                    settings.InstallPath,
                    progress => ProgressBarMain.Value = progress,
                    log => TxtLog.AppendText(log + "\n")
                );

                TxtStatus.Text = "インストールが完了しました！";
                MessageBox.Show("Godot Engineのインストールが完了しました！", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "エラーが発生しました";
                TxtLog.AppendText($"[致命的なエラー] {ex.Message}\n");
                MessageBox.Show($"インストール中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnStartInstall.IsEnabled = true;
            }
        }

    }
}