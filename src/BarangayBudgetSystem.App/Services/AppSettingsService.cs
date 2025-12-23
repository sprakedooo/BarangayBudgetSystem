using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using BarangayBudgetSystem.App.Helpers;

namespace BarangayBudgetSystem.App.Services
{
    public interface IAppSettingsService
    {
        AppSettings Settings { get; }
        void LoadSettings();
        void SaveSettings();
        void ApplySidebarColor(string colorHex);
    }

    public class AppSettings
    {
        public string BarangayName { get; set; } = "Barangay Sample";
        public string MunicipalityName { get; set; } = "Municipality Sample";
        public string ProvinceName { get; set; } = "Province Sample";
        public int DefaultFiscalYear { get; set; } = DateTime.Now.Year;
        public bool AutoBackupEnabled { get; set; } = false;
        public int BackupRetentionDays { get; set; } = 30;
        public string SelectedTheme { get; set; } = "Light";
        public string SidebarColor { get; set; } = "#2c3e50"; // Default dark blue
        public string SidebarGradientEnd { get; set; } = "#1a252f";
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly string _settingsPath;
        private readonly IEventBus _eventBus;

        public AppSettings Settings { get; private set; }

        public AppSettingsService(IEventBus eventBus)
        {
            _eventBus = eventBus;
            Settings = new AppSettings();

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BarangayBudgetSystem");

            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "appsettings.json");

            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                Settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public void ApplySidebarColor(string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);

                // Create a darker version for the gradient end
                var darkerColor = Color.FromRgb(
                    (byte)Math.Max(0, color.R - 30),
                    (byte)Math.Max(0, color.G - 30),
                    (byte)Math.Max(0, color.B - 30));

                Settings.SidebarColor = colorHex;
                Settings.SidebarGradientEnd = $"#{darkerColor.R:X2}{darkerColor.G:X2}{darkerColor.B:X2}";

                // Update the application resource
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                gradient.GradientStops.Add(new GradientStop(color, 0));
                gradient.GradientStops.Add(new GradientStop(darkerColor, 1));

                if (Application.Current.Resources.Contains("SidebarGradientBrush"))
                {
                    Application.Current.Resources["SidebarGradientBrush"] = gradient;
                }

                SaveSettings();

                // Publish event to notify of sidebar color change
                _eventBus.Publish(new SidebarColorChangedEvent { NewColor = colorHex });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply sidebar color: {ex.Message}");
            }
        }
    }

    public class SidebarColorChangedEvent
    {
        public string NewColor { get; set; } = string.Empty;
    }

    public class SidebarColorOption
    {
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public Brush ColorBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorHex));
    }
}
