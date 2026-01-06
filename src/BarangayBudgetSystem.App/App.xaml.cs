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

            // Show login window first
            ShowLoginWindow();
        }

        private void ShowLoginWindow()
        {
            var loginWindow = _serviceProvider?.GetRequiredService<LoginWindow>();
            var loginViewModel = _serviceProvider?.GetRequiredService<LoginViewModel>();

            if (loginWindow != null && loginViewModel != null)
            {
                loginViewModel.LoginSuccessful += () =>
                {
                    loginWindow.Hide();
                    ShowMainWindow();
                    loginWindow.Close();
                };

                loginWindow.DataContext = loginViewModel;
                loginWindow.Show();
            }
        }

        private void ShowMainWindow()
        {
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
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IFundService, FundService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IReportGenerationService, ReportGenerationService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IBackupService, BackupService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<TransactionsViewModel>();
            services.AddTransient<FundsViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<DocumentsViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
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
                        // Use a separate connection string directly to avoid EF issues
                        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                        connection.Open();

                        // Check for Funds table
                        using var command = connection.CreateCommand();
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Funds';";
                        var fundsResult = command.ExecuteScalar();

                        // Check for FundParticulars table
                        using var command2 = connection.CreateCommand();
                        command2.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FundParticulars';";
                        var particularsResult = command2.ExecuteScalar();

                        // Check for FundParticularId column in Transactions table
                        using var command3 = connection.CreateCommand();
                        command3.CommandText = "PRAGMA table_info(Transactions);";
                        var hasParticularIdColumn = false;
                        using (var reader = command3.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var columnName = reader.GetString(1);
                                if (columnName == "FundParticularId")
                                {
                                    hasParticularIdColumn = true;
                                    break;
                                }
                            }
                        }

                        connection.Close();

                        if (fundsResult == null || particularsResult == null || !hasParticularIdColumn)
                        {
                            needsRecreate = true;
                            System.Diagnostics.Debug.WriteLine("Required tables/columns not found. Will recreate database.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Database schema is up to date.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error checking database: {ex.Message}");
                        needsRecreate = true;
                    }

                    if (needsRecreate)
                    {
                        // Delete the old database file
                        try
                        {
                            // Close any open connections first
                            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                            System.IO.File.Delete(dbPath);
                            System.Diagnostics.Debug.WriteLine("Deleted old database file for schema update.");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not delete database file: {ex.Message}");
                            MessageBox.Show(
                                $"Please close the application and manually delete the database file at:\n{dbPath}\n\nThen restart the application.",
                                "Database Update Required",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
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

                        // Ensure admin user exists with correct password
                        EnsureAdminUser(context);
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

        private void EnsureAdminUser(AppDbContext context)
        {
            try
            {
                // Check if admin user exists
                var adminExists = context.Users.Any(u => u.Username.ToLower() == "admin");
                if (!adminExists)
                {
                    // Get the authentication service to hash the password
                    var authService = _serviceProvider?.GetService<IAuthenticationService>();
                    if (authService != null)
                    {
                        var adminUser = new Models.User
                        {
                            Username = "admin",
                            PasswordHash = authService.HashPassword("admin123"),
                            FirstName = "System",
                            LastName = "Administrator",
                            Role = Models.UserRoles.Administrator,
                            Position = "System Administrator",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        context.Users.Add(adminUser);
                        context.SaveChanges();
                        System.Diagnostics.Debug.WriteLine("Admin user created successfully.");
                    }
                }
                else
                {
                    // Update admin password hash if needed (for existing databases)
                    var admin = context.Users.FirstOrDefault(u => u.Username.ToLower() == "admin");
                    if (admin != null)
                    {
                        var authService = _serviceProvider?.GetService<IAuthenticationService>();
                        if (authService != null)
                        {
                            var correctHash = authService.HashPassword("admin123");
                            if (admin.PasswordHash != correctHash)
                            {
                                admin.PasswordHash = correctHash;
                                context.SaveChanges();
                                System.Diagnostics.Debug.WriteLine("Admin password hash updated.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring admin user: {ex.Message}");
            }
        }
    }
}
