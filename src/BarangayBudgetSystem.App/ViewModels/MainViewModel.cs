using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IEventBus _eventBus;
        private readonly IServiceScopeFactory _scopeFactory;
        private BaseViewModel? _currentViewModel;
        private string _currentViewName = "Dashboard";
        private User? _currentUser;
        private string _applicationTitle = "Barangay Budget System";
        private int _currentFiscalYear;

        // Store current scope to dispose when switching views
        private IServiceScope? _currentScope;

        public MainViewModel(
            IEventBus eventBus,
            IServiceScopeFactory scopeFactory)
        {
            _eventBus = eventBus;
            _scopeFactory = scopeFactory;
            _currentFiscalYear = DateTime.Now.Year;

            // Initialize commands
            NavigateToDashboardCommand = new RelayCommand(() => NavigateTo("Dashboard"));
            NavigateToBudgetSetupCommand = new RelayCommand(() => NavigateTo("BudgetSetup"));
            NavigateToTransactionsCommand = new RelayCommand(() => NavigateTo("Transactions"));
            NavigateToFundsCommand = new RelayCommand(() => NavigateTo("Funds"));
            NavigateToReportsCommand = new RelayCommand(() => NavigateTo("Reports"));
            NavigateToDocumentsCommand = new RelayCommand(() => NavigateTo("Documents"));
            NavigateToSettingsCommand = new RelayCommand(() => NavigateTo("Settings"));
            LogoutCommand = new RelayCommand(Logout);

            // Subscribe to navigation events
            _eventBus.Subscribe<NavigationEvent>(OnNavigationRequested);

            // Set default view (will create fresh scope)
            NavigateTo("Dashboard");
        }

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentViewName
        {
            get => _currentViewName;
            set => SetProperty(ref _currentViewName, value);
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                SetProperty(ref _currentUser, value);
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(UserDisplayName));
                OnPropertyChanged(nameof(UserRole));
            }
        }

        public bool IsLoggedIn => CurrentUser != null;

        public string UserDisplayName => CurrentUser?.FullName ?? "Guest";

        public string UserRole => CurrentUser?.Role ?? string.Empty;

        public string ApplicationTitle
        {
            get => _applicationTitle;
            set => SetProperty(ref _applicationTitle, value);
        }

        public int CurrentFiscalYear
        {
            get => _currentFiscalYear;
            set
            {
                if (SetProperty(ref _currentFiscalYear, value))
                {
                    OnFiscalYearChanged();
                }
            }
        }

        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToBudgetSetupCommand { get; }
        public ICommand NavigateToTransactionsCommand { get; }
        public ICommand NavigateToFundsCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToDocumentsCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public bool IsDashboardSelected => CurrentViewName == "Dashboard";
        public bool IsBudgetSetupSelected => CurrentViewName == "BudgetSetup";
        public bool IsTransactionsSelected => CurrentViewName == "Transactions";
        public bool IsFundsSelected => CurrentViewName == "Funds";
        public bool IsReportsSelected => CurrentViewName == "Reports";
        public bool IsDocumentsSelected => CurrentViewName == "Documents";
        public bool IsSettingsSelected => CurrentViewName == "Settings";

        public void NavigateTo(string viewName, object? parameter = null)
        {
            // Cleanup and dispose the previous scope to release DbContext
            _currentViewModel?.Cleanup();
            _currentScope?.Dispose();

            // Create a new scope for this view - this gives fresh DbContext and services
            _currentScope = _scopeFactory.CreateScope();
            var provider = _currentScope.ServiceProvider;

            CurrentViewName = viewName;
            CurrentViewModel = viewName switch
            {
                "Dashboard" => provider.GetRequiredService<DashboardViewModel>(),
                "BudgetSetup" => provider.GetRequiredService<BudgetSetupViewModel>(),
                "Transactions" => provider.GetRequiredService<TransactionsViewModel>(),
                "Funds" => provider.GetRequiredService<FundsViewModel>(),
                "Reports" => provider.GetRequiredService<ReportsViewModel>(),
                "Documents" => provider.GetRequiredService<DocumentsViewModel>(),
                "Settings" => provider.GetRequiredService<SettingsViewModel>(),
                _ => provider.GetRequiredService<DashboardViewModel>()
            };

            // Notify view selection changes
            OnPropertyChanged(nameof(IsDashboardSelected));
            OnPropertyChanged(nameof(IsBudgetSetupSelected));
            OnPropertyChanged(nameof(IsTransactionsSelected));
            OnPropertyChanged(nameof(IsFundsSelected));
            OnPropertyChanged(nameof(IsReportsSelected));
            OnPropertyChanged(nameof(IsDocumentsSelected));
            OnPropertyChanged(nameof(IsSettingsSelected));

            // Initialize the selected view
            CurrentViewModel?.InitializeAsync();
        }

        private void OnNavigationRequested(NavigationEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.ViewName))
            {
                NavigateTo(evt.ViewName, evt.Parameter);
            }
        }

        private void OnFiscalYearChanged()
        {
            // Refresh all views with new fiscal year
            _eventBus.Publish(new DashboardRefreshEvent
            {
                RefreshFunds = true,
                RefreshTransactions = true,
                RefreshCharts = true
            });
        }

        public void SetCurrentUser(User user)
        {
            CurrentUser = user;
            _eventBus.Publish(new UserLoggedInEvent
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role
            });
        }

        private void Logout()
        {
            if (CurrentUser != null)
            {
                _eventBus.Publish(new UserLoggedOutEvent
                {
                    UserId = CurrentUser.Id,
                    Username = CurrentUser.Username
                });
            }

            CurrentUser = null;
            NavigateTo("Dashboard");
        }

        public override async System.Threading.Tasks.Task InitializeAsync()
        {
            // Already initialized in constructor via NavigateTo
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public override void Cleanup()
        {
            _currentViewModel?.Cleanup();
            _currentScope?.Dispose();
            _eventBus.Unsubscribe<NavigationEvent>(OnNavigationRequested);
            base.Cleanup();
        }
    }
}
