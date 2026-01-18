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
        void ApplyTheme(string theme);
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

        public void ApplyTheme(string theme)
        {
            try
            {
                Settings.SelectedTheme = theme;
                var isDarkMode = theme == "Dark";

                // Define theme colors
                var colors = isDarkMode ? GetDarkThemeColors() : GetLightThemeColors();

                // Apply background colors
                UpdateBrush("BackgroundBrush", colors.Background);
                UpdateBrush("CardBackgroundBrush", colors.CardBackground);
                UpdateBrush("HeaderBackgroundBrush", colors.HeaderBackground);

                // Apply text colors
                UpdateBrush("TextPrimaryBrush", colors.TextPrimary);
                UpdateBrush("TextSecondaryBrush", colors.TextSecondary);
                UpdateBrush("TextMutedBrush", colors.TextMuted);

                // Apply border colors
                UpdateBrush("BorderBrush", colors.Border);
                UpdateBrush("BorderLightBrush", colors.BorderLight);

                // Apply input colors
                UpdateBrush("InputBackgroundBrush", colors.InputBackground);
                UpdateBrush("InputBorderBrush", colors.InputBorder);
                UpdateBrush("InputTextBrush", colors.InputText);
                UpdateBrush("InputPlaceholderBrush", colors.InputPlaceholder);

                // Apply DataGrid colors
                UpdateBrush("DataGridBackgroundBrush", colors.DataGridBackground);
                UpdateBrush("DataGridAlternateRowBrush", colors.DataGridAlternateRow);
                UpdateBrush("DataGridHeaderBrush", colors.DataGridHeader);
                UpdateBrush("DataGridBorderBrush", colors.DataGridBorder);
                UpdateBrush("DataGridRowHoverBrush", colors.DataGridRowHover);
                UpdateBrush("DataGridSelectedRowBrush", colors.DataGridSelectedRow);

                SaveSettings();

                // Publish event to notify of theme change
                _eventBus.Publish(new ThemeChangedEvent { Theme = theme, IsDarkMode = isDarkMode });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
            }
        }

        private void UpdateBrush(string resourceKey, string colorHex)
        {
            if (Application.Current.Resources.Contains(resourceKey))
            {
                Application.Current.Resources[resourceKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }
        }

        private ThemeColors GetLightThemeColors()
        {
            return new ThemeColors
            {
                Background = "#f8f9fa",
                CardBackground = "#ffffff",
                HeaderBackground = "#ffffff",
                TextPrimary = "#212529",
                TextSecondary = "#6c757d",
                TextMuted = "#adb5bd",
                Border = "#dee2e6",
                BorderLight = "#e9ecef",
                InputBackground = "#ffffff",
                InputBorder = "#dee2e6",
                InputText = "#212529",
                InputPlaceholder = "#6c757d",
                DataGridBackground = "#ffffff",
                DataGridAlternateRow = "#f8f9fa",
                DataGridHeader = "#e9ecef",
                DataGridBorder = "#dee2e6",
                DataGridRowHover = "#e9ecef",
                DataGridSelectedRow = "#cce5ff"
            };
        }

        private ThemeColors GetDarkThemeColors()
        {
            return new ThemeColors
            {
                Background = "#1a1d21",
                CardBackground = "#2d3238",
                HeaderBackground = "#2d3238",
                TextPrimary = "#e9ecef",
                TextSecondary = "#adb5bd",
                TextMuted = "#6c757d",
                Border = "#495057",
                BorderLight = "#3d4349",
                InputBackground = "#3d4349",
                InputBorder = "#495057",
                InputText = "#e9ecef",
                InputPlaceholder = "#adb5bd",
                DataGridBackground = "#2d3238",
                DataGridAlternateRow = "#343a40",
                DataGridHeader = "#495057",
                DataGridBorder = "#495057",
                DataGridRowHover = "#495057",
                DataGridSelectedRow = "#3d5a80"
            };
        }
    }

    public class ThemeColors
    {
        public string Background { get; set; } = string.Empty;
        public string CardBackground { get; set; } = string.Empty;
        public string HeaderBackground { get; set; } = string.Empty;
        public string TextPrimary { get; set; } = string.Empty;
        public string TextSecondary { get; set; } = string.Empty;
        public string TextMuted { get; set; } = string.Empty;
        public string Border { get; set; } = string.Empty;
        public string BorderLight { get; set; } = string.Empty;
        public string InputBackground { get; set; } = string.Empty;
        public string InputBorder { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string InputPlaceholder { get; set; } = string.Empty;
        public string DataGridBackground { get; set; } = string.Empty;
        public string DataGridAlternateRow { get; set; } = string.Empty;
        public string DataGridHeader { get; set; } = string.Empty;
        public string DataGridBorder { get; set; } = string.Empty;
        public string DataGridRowHover { get; set; } = string.Empty;
        public string DataGridSelectedRow { get; set; } = string.Empty;
    }

    public class ThemeChangedEvent
    {
        public string Theme { get; set; } = string.Empty;
        public bool IsDarkMode { get; set; }
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
