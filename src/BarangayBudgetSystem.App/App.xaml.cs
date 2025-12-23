using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BarangayBudgetSystem.App.Data;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Services;
using BarangayBudgetSystem.App.ViewModels;
using BarangayBudgetSystem.App.Views;

namespace BarangayBudgetSystem.App
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public App()
        {
            _serviceProvider = ConfigureServices();
            Services = _serviceProvider;
        }

        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize database
            InitializeDatabase();

            // Apply saved sidebar color
            ApplySavedSidebarColor();

            // Create and show main window
            var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
            if (mainWindow != null)
            {
                mainWindow.DataContext = _serviceProvider?.GetRequiredService<MainViewModel>();
                mainWindow.Show();
            }
        }

        private void ApplySavedSidebarColor()
        {
            try
            {
                var settingsService = _serviceProvider?.GetService<IAppSettingsService>();
                if (settingsService != null && !string.IsNullOrEmpty(settingsService.Settings.SidebarColor))
                {
                    settingsService.ApplySidebarColor(settingsService.Settings.SidebarColor);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply sidebar color: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _serviceProvider?.Dispose();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Database
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BarangayBudgetSystem",
                "budget.db");
            var dbDir = System.IO.Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir))
            {
                System.IO.Directory.CreateDirectory(dbDir);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Services
            services.AddSingleton<IEventBus, EventBus>();
            services.AddSingleton<IDialogHelper, DialogHelper>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddScoped<IFundService, FundService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IReportGenerationService, ReportGenerationService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IBackupService, BackupService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<TransactionsViewModel>();
            services.AddTransient<FundsViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<DocumentsViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<DashboardView>();
            services.AddTransient<TransactionsView>();
            services.AddTransient<FundsView>();
            services.AddTransient<ReportsView>();
            services.AddTransient<DocumentsView>();
            services.AddTransient<SettingsView>();

            return services.BuildServiceProvider();
        }

        private void InitializeDatabase()
        {
            try
            {
                // Get the database path
                var dbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BarangayBudgetSystem",
                    "budget.db");

                System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}");

                // Ensure directory exists
                var dbDir = System.IO.Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
                {
                    System.IO.Directory.CreateDirectory(dbDir);
                }

                // Check if we need to delete and recreate
                bool needsRecreate = false;

                if (System.IO.File.Exists(dbPath))
                {
                    // Try to connect and check if tables exist
                    try
                    {
                        using var scope = _serviceProvider?.CreateScope();
                        var context = scope?.ServiceProvider.GetRequiredService<AppDbContext>();
                        if (context != null)
                        {
                            // Try a raw SQL query to check if Funds table exists
                            var connection = context.Database.GetDbConnection();
                            connection.Open();
                            using var command = connection.CreateCommand();
                            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Funds';";
                            var result = command.ExecuteScalar();
                            connection.Close();

                            if (result == null)
                            {
                                needsRecreate = true;
                                System.Diagnostics.Debug.WriteLine("Funds table not found. Will recreate database.");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Database tables exist.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error checking database: {ex.Message}");
                        needsRecreate = true;
                    }

                    if (needsRecreate)
                    {
                        // Delete the corrupted/empty database file
                        try
                        {
                            System.IO.File.Delete(dbPath);
                            System.Diagnostics.Debug.WriteLine("Deleted old database file.");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not delete database file: {ex.Message}");
                        }
                    }
                }

                // Now create the database with tables
                using (var scope = _serviceProvider?.CreateScope())
                {
                    var context = scope?.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (context != null)
                    {
                        var created = context.Database.EnsureCreated();
                        System.Diagnostics.Debug.WriteLine(created
                            ? "Database created successfully with all tables."
                            : "Database already exists with tables.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database: {ex.Message}\n\nDetails: {ex.InnerException?.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex}");
            }
        }
    }
}
