using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace GodotInstaller
{
    public class AppSettings
    {
        // 設定ファイルの保存先（例: AppData\Local\GodotInstaller\settings.json）
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GodotInstaller",
            "settings.json"
        );

        // --- 設定プロパティ ---
        public bool IsDarkMode { get; set; } = true;
        public bool IsNativeFrame { get; set; } = false;
        public string InstallPath { get; set; } = @"C:\Godot";
        public string SelectedVersion { get; set; } = "latest";

        /// <path>設定をJSONファイルから読み込む</path>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string jsonString = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(jsonString) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定の読み込みに失敗しました: {ex.Message}");
            }

            // ファイルがない、または読み込み失敗時はデフォルト値を返す
            return new AppSettings();
        }

        /// <path>設定をJSONファイルに保存する</path>
        public void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true }; // 綺麗なインデント付きJSON
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message} \n {ex.GetType().FullName} \n {ex.HResult:X8}");
                return;
            }
        }
    }
}
