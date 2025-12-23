using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Services;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IBackupService _backupService;
        private readonly IDialogHelper _dialogHelper;
        private readonly IEventBus _eventBus;
        private readonly IAppSettingsService _appSettingsService;

        private string _barangayName = "Barangay Sample";
        private string _municipalityName = "Municipality Sample";
        private string _provinceName = "Province Sample";
        private int _defaultFiscalYear;
        private bool _autoBackupEnabled;
        private int _backupRetentionDays = 30;
        private string? _selectedTheme = "Light";
        private SidebarColorOption? _selectedSidebarColor;
        private Color _customSidebarColor = Color.FromRgb(44, 62, 80); // Default dark blue

        public SettingsViewModel(
            IBackupService backupService,
            IDialogHelper dialogHelper,
            IEventBus eventBus,
            IAppSettingsService appSettingsService)
        {
            _backupService = backupService;
            _dialogHelper = dialogHelper;
            _eventBus = eventBus;
            _appSettingsService = appSettingsService;
            _defaultFiscalYear = DateTime.Now.Year;

            Backups = new ObservableCollection<BackupInfo>();
            ThemeOptions = new ObservableCollection<string> { "Light", "Dark", "System" };

            // Initialize sidebar color options
            SidebarColorOptions = new ObservableCollection<SidebarColorOption>
            {
                new SidebarColorOption { Name = "Dark Blue (Default)", ColorHex = "#2c3e50" },
                new SidebarColorOption { Name = "Navy Blue", ColorHex = "#1a365d" },
                new SidebarColorOption { Name = "Midnight Blue", ColorHex = "#191970" },
                new SidebarColorOption { Name = "Dark Slate", ColorHex = "#2f4f4f" },
                new SidebarColorOption { Name = "Charcoal", ColorHex = "#36454f" },
                new SidebarColorOption { Name = "Dark Gray", ColorHex = "#343a40" },
                new SidebarColorOption { Name = "Forest Green", ColorHex = "#228b22" },
                new SidebarColorOption { Name = "Dark Green", ColorHex = "#1e5631" },
                new SidebarColorOption { Name = "Teal", ColorHex = "#008080" },
                new SidebarColorOption { Name = "Dark Cyan", ColorHex = "#0e4d64" },
                new SidebarColorOption { Name = "Maroon", ColorHex = "#800000" },
                new SidebarColorOption { Name = "Dark Red", ColorHex = "#8b0000" },
                new SidebarColorOption { Name = "Purple", ColorHex = "#4b0082" },
                new SidebarColorOption { Name = "Dark Violet", ColorHex = "#483d8b" },
                new SidebarColorOption { Name = "Brown", ColorHex = "#5d4037" },
                new SidebarColorOption { Name = "Coffee", ColorHex = "#4a3728" }
            };

            // Commands
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
            RestoreBackupCommand = new AsyncRelayCommand<BackupInfo>(RestoreBackupAsync);
            DeleteBackupCommand = new AsyncRelayCommand<BackupInfo>(DeleteBackupAsync);
            LoadBackupsCommand = new AsyncRelayCommand(LoadBackupsAsync);
            CleanupBackupsCommand = new AsyncRelayCommand(CleanupBackupsAsync);
            OpenBackupFolderCommand = new RelayCommand(OpenBackupFolder);
            ExportSettingsCommand = new RelayCommand(ExportSettings);
            ImportSettingsCommand = new RelayCommand(ImportSettings);
            ApplySidebarColorCommand = new RelayCommand<SidebarColorOption>(ApplySidebarColor);
            ApplyCustomColorCommand = new RelayCommand(ApplyCustomColor);
        }

        public ObservableCollection<BackupInfo> Backups { get; }
        public ObservableCollection<string> ThemeOptions { get; }
        public ObservableCollection<SidebarColorOption> SidebarColorOptions { get; }

        public string BarangayName
        {
            get => _barangayName;
            set => SetProperty(ref _barangayName, value);
        }

        public string MunicipalityName
        {
            get => _municipalityName;
            set => SetProperty(ref _municipalityName, value);
        }

        public string ProvinceName
        {
            get => _provinceName;
            set => SetProperty(ref _provinceName, value);
        }

        public int DefaultFiscalYear
        {
            get => _defaultFiscalYear;
            set => SetProperty(ref _defaultFiscalYear, value);
        }

        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set => SetProperty(ref _autoBackupEnabled, value);
        }

        public int BackupRetentionDays
        {
            get => _backupRetentionDays;
            set => SetProperty(ref _backupRetentionDays, value);
        }

        public string? SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        public SidebarColorOption? SelectedSidebarColor
        {
            get => _selectedSidebarColor;
            set
            {
                if (SetProperty(ref _selectedSidebarColor, value) && value != null)
                {
                    ApplySidebarColor(value);
                }
            }
        }

        public Color CustomSidebarColor
        {
            get => _customSidebarColor;
            set
            {
                if (SetProperty(ref _customSidebarColor, value))
                {
                    // Clear preset selection when using custom color
                    _selectedSidebarColor = null;
                    OnPropertyChanged(nameof(SelectedSidebarColor));
                }
            }
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand DeleteBackupCommand { get; }
        public ICommand LoadBackupsCommand { get; }
        public ICommand CleanupBackupsCommand { get; }
        public ICommand OpenBackupFolderCommand { get; }
        public ICommand ExportSettingsCommand { get; }
        public ICommand ImportSettingsCommand { get; }
        public ICommand ApplySidebarColorCommand { get; }
        public ICommand ApplyCustomColorCommand { get; }

        public override async Task InitializeAsync()
        {
            LoadSettingsFromConfig();
            await LoadBackupsAsync();
        }

        private void LoadSettingsFromConfig()
        {
            // Load settings from AppSettingsService
            var settings = _appSettingsService.Settings;
            BarangayName = settings.BarangayName;
            MunicipalityName = settings.MunicipalityName;
            ProvinceName = settings.ProvinceName;
            DefaultFiscalYear = settings.DefaultFiscalYear;
            AutoBackupEnabled = settings.AutoBackupEnabled;
            BackupRetentionDays = settings.BackupRetentionDays;
            SelectedTheme = settings.SelectedTheme;

            // Find and set the current sidebar color
            bool foundPreset = false;
            foreach (var colorOption in SidebarColorOptions)
            {
                if (colorOption.ColorHex.Equals(settings.SidebarColor, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedSidebarColor = colorOption;
                    OnPropertyChanged(nameof(SelectedSidebarColor));
                    foundPreset = true;
                    break;
                }
            }

            // If not a preset, set the custom color
            if (!foundPreset && !string.IsNullOrEmpty(settings.SidebarColor))
            {
                try
                {
                    _customSidebarColor = (Color)ColorConverter.ConvertFromString(settings.SidebarColor);
                    OnPropertyChanged(nameof(CustomSidebarColor));
                }
                catch
                {
                    // Use default if parsing fails
                }
            }
        }

        private void SaveSettings()
        {
            // Save settings to AppSettingsService
            var settings = _appSettingsService.Settings;
            settings.BarangayName = BarangayName;
            settings.MunicipalityName = MunicipalityName;
            settings.ProvinceName = ProvinceName;
            settings.DefaultFiscalYear = DefaultFiscalYear;
            settings.AutoBackupEnabled = AutoBackupEnabled;
            settings.BackupRetentionDays = BackupRetentionDays;
            settings.SelectedTheme = SelectedTheme ?? "Light";

            _appSettingsService.SaveSettings();
            ShowMessage("Settings saved successfully.");
        }

        private void ApplySidebarColor(SidebarColorOption? colorOption)
        {
            if (colorOption == null) return;

            _appSettingsService.ApplySidebarColor(colorOption.ColorHex);
        }

        private void ApplyCustomColor()
        {
            var colorHex = $"#{CustomSidebarColor.R:X2}{CustomSidebarColor.G:X2}{CustomSidebarColor.B:X2}";
            _appSettingsService.ApplySidebarColor(colorHex);
        }

        private async Task LoadBackupsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var backups = await _backupService.GetAvailableBackupsAsync();
                Backups.Clear();
                foreach (var backup in backups)
                {
                    Backups.Add(backup);
                }
            }, "Loading backups...");
        }

        private async Task CreateBackupAsync()
        {
            await ExecuteAsync(async () =>
            {
                var backupPath = await _backupService.CreateBackupAsync();
                ShowMessage($"Backup created successfully:\n{backupPath}");
                await LoadBackupsAsync();
            }, "Creating backup...");
        }

        private async Task RestoreBackupAsync(BackupInfo? backup)
        {
            if (backup == null) return;

            if (!ShowConfirmation($"Are you sure you want to restore from backup '{backup.FileName}'?\n\nThis will replace all current data. A backup of the current state will be created first."))
                return;

            await ExecuteAsync(async () =>
            {
                await _backupService.RestoreBackupAsync(backup.FilePath);
                ShowMessage("Backup restored successfully. Please restart the application.");
            }, "Restoring backup...");
        }

        private async Task DeleteBackupAsync(BackupInfo? backup)
        {
            if (backup == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete backup '{backup.FileName}'?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _backupService.DeleteBackupAsync(backup.FilePath);
                ShowMessage("Backup deleted successfully.");
                await LoadBackupsAsync();
            }, "Deleting backup...");
        }

        private async Task CleanupBackupsAsync()
        {
            var keepCount = BackupRetentionDays > 0 ? BackupRetentionDays / 3 : 10;

            if (!ShowConfirmation($"This will delete all backups except the {keepCount} most recent ones. Continue?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _backupService.CleanupOldBackupsAsync(keepCount);
                ShowMessage("Old backups cleaned up successfully.");
                await LoadBackupsAsync();
            }, "Cleaning up backups...");
        }

        private void OpenBackupFolder()
        {
            var folderPath = _backupService.GetBackupFolder();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }

        private void ExportSettings()
        {
            var filePath = _dialogHelper.ShowSaveFileDialog(
                "JSON files (*.json)|*.json",
                "Export Settings",
                "settings_export.json");

            if (string.IsNullOrEmpty(filePath)) return;

            // Export settings to JSON file
            var settings = new
            {
                BarangayName,
                MunicipalityName,
                ProvinceName,
                DefaultFiscalYear,
                AutoBackupEnabled,
                BackupRetentionDays,
                SelectedTheme
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            System.IO.File.WriteAllText(filePath, json);
            ShowMessage("Settings exported successfully.");
        }

        private void ImportSettings()
        {
            var filePath = _dialogHelper.ShowOpenFileDialog(
                "JSON files (*.json)|*.json",
                "Import Settings");

            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);

                if (settings != null)
                {
                    BarangayName = settings.BarangayName ?? BarangayName;
                    MunicipalityName = settings.MunicipalityName ?? MunicipalityName;
                    ProvinceName = settings.ProvinceName ?? ProvinceName;
                    DefaultFiscalYear = settings.DefaultFiscalYear;
                    AutoBackupEnabled = settings.AutoBackupEnabled;
                    BackupRetentionDays = settings.BackupRetentionDays;
                    SelectedTheme = settings.SelectedTheme ?? SelectedTheme;

                    ShowMessage("Settings imported successfully.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to import settings: {ex.Message}");
            }
        }

        private class SettingsData
        {
            public string? BarangayName { get; set; }
            public string? MunicipalityName { get; set; }
            public string? ProvinceName { get; set; }
            public int DefaultFiscalYear { get; set; }
            public bool AutoBackupEnabled { get; set; }
            public int BackupRetentionDays { get; set; }
            public string? SelectedTheme { get; set; }
        }
    }
}
