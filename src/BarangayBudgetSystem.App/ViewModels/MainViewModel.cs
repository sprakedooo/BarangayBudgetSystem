using System;
using System.Windows.Input;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IEventBus _eventBus;
        private BaseViewModel? _currentViewModel;
        private string _currentViewName = "Dashboard";
        private User? _currentUser;
        private string _applicationTitle = "Barangay Budget System";
        private int _currentFiscalYear;

        public MainViewModel(
            IEventBus eventBus,
            DashboardViewModel dashboardViewModel,
            TransactionsViewModel transactionsViewModel,
            FundsViewModel fundsViewModel,
            ReportsViewModel reportsViewModel,
            DocumentsViewModel documentsViewModel,
            SettingsViewModel settingsViewModel)
        {
            _eventBus = eventBus;
            _currentFiscalYear = DateTime.Now.Year;

            DashboardViewModel = dashboardViewModel;
            TransactionsViewModel = transactionsViewModel;
            FundsViewModel = fundsViewModel;
            ReportsViewModel = reportsViewModel;
            DocumentsViewModel = documentsViewModel;
            SettingsViewModel = settingsViewModel;

            // Set default view
            CurrentViewModel = DashboardViewModel;

            // Initialize commands
            NavigateToDashboardCommand = new RelayCommand(() => NavigateTo("Dashboard"));
            NavigateToTransactionsCommand = new RelayCommand(() => NavigateTo("Transactions"));
            NavigateToFundsCommand = new RelayCommand(() => NavigateTo("Funds"));
            NavigateToReportsCommand = new RelayCommand(() => NavigateTo("Reports"));
            NavigateToDocumentsCommand = new RelayCommand(() => NavigateTo("Documents"));
            NavigateToSettingsCommand = new RelayCommand(() => NavigateTo("Settings"));
            LogoutCommand = new RelayCommand(Logout);

            // Subscribe to navigation events
            _eventBus.Subscribe<NavigationEvent>(OnNavigationRequested);
        }

        public DashboardViewModel DashboardViewModel { get; }
        public TransactionsViewModel TransactionsViewModel { get; }
        public FundsViewModel FundsViewModel { get; }
        public ReportsViewModel ReportsViewModel { get; }
        public DocumentsViewModel DocumentsViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

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
        public ICommand NavigateToTransactionsCommand { get; }
        public ICommand NavigateToFundsCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToDocumentsCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public bool IsDashboardSelected => CurrentViewName == "Dashboard";
        public bool IsTransactionsSelected => CurrentViewName == "Transactions";
        public bool IsFundsSelected => CurrentViewName == "Funds";
        public bool IsReportsSelected => CurrentViewName == "Reports";
        public bool IsDocumentsSelected => CurrentViewName == "Documents";
        public bool IsSettingsSelected => CurrentViewName == "Settings";

        public void NavigateTo(string viewName, object? parameter = null)
        {
            CurrentViewName = viewName;
            CurrentViewModel = viewName switch
            {
                "Dashboard" => DashboardViewModel,
                "Transactions" => TransactionsViewModel,
                "Funds" => FundsViewModel,
                "Reports" => ReportsViewModel,
                "Documents" => DocumentsViewModel,
                "Settings" => SettingsViewModel,
                _ => DashboardViewModel
            };

            // Notify view selection changes
            OnPropertyChanged(nameof(IsDashboardSelected));
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
            await DashboardViewModel.InitializeAsync();
        }

        public override void Cleanup()
        {
            _eventBus.Unsubscribe<NavigationEvent>(OnNavigationRequested);
            base.Cleanup();
        }
    }
}
