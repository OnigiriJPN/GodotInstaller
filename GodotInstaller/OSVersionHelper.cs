using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GodotInstaller
{
    public static class OSVersionHelper
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RTL_OSVERSIONINFOEX
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        /// <summary>
        /// マニフェストに騙されず、ntdllから実際のOSバージョンとビルド番号を取得する
        /// </summary>
        public static (uint Major, uint Minor, uint Build) GetRealOSVersion()
        {
            var vi = new RTL_OSVERSIONINFOEX();
            vi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(RTL_OSVERSIONINFOEX));

            if (RtlGetVersion(ref vi) == 0)
            {
                return (vi.dwMajorVersion, vi.dwMinorVersion, vi.dwBuildNumber);
            }

            // フォールバック
            return (0, 0, 0);
        }

        /// <summary>
        /// Windows 11以降か判定する（ビルド番号 22000 以上）
        /// </summary>
        public static bool IsWindows11OrGreater()
        {
            var (major, minor, build) = GetRealOSVersion();
            // Windows 11 は Major 10 で、ビルド番号が 22000 以降
            return major >= 10 && build >= 22000;
        }
    }
}
