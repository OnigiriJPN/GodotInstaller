using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace GodotInstaller
{
    public static class MicaManager
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // Windows 11のシステム背景効果用の定数
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // 背景タイプの列挙
        private enum BackdropType
        {
            Auto = 0,
            None = 1,
            MainWindow = 2,    // Mica
            TransientWindow = 3, // Acrylic
            TabbedWindow = 4   // Mica Alt
        }
        public static void EnableMicaIfSupported(Window TargetWindow)
        {
            // 本物判定で Windows 11 以上（ビルド22000〜）のときだけMicaを有効化
            if (OSVersionHelper.IsWindows11OrGreater())
            {
                IntPtr hWnd = new WindowInteropHelper(TargetWindow).Handle;
                if (hWnd != IntPtr.Zero)
                {
                    int backdropType = 2; // 2 = MainWindow (Mica)
                    DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                }
            }
            else
            {
                throw new PlatformNotSupportedException("お使いのOSバージョンはWindows 11じゃないようです。Mica効果はWindows 11以降使用できます。");
            }
        }
    }
}
