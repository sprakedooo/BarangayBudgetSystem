using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IFundService _fundService;
        private readonly ITransactionService _transactionService;
        private readonly IEventBus _eventBus;

        private decimal _totalBudget;
        private decimal _totalExpenses;
        private decimal _totalRemaining;
        private double _overallUtilization;
        private int _totalFunds;
        private int _pendingApprovals;
        private int _currentFiscalYear;
        private ObservableCollection<int> _fiscalYears;

        public DashboardViewModel(
            IFundService fundService,
            ITransactionService transactionService,
            IEventBus eventBus)
        {
            _fundService = fundService;
            _transactionService = transactionService;
            _eventBus = eventBus;
            _currentFiscalYear = DateTime.Now.Year;

            Funds = new ObservableCollection<AppropriationFund>();
            RecentTransactions = new ObservableCollection<Transaction>();
            LowBalanceFunds = new ObservableCollection<AppropriationFund>();
            CategorySummaries = new ObservableCollection<FundCategorySummary>();

            // Initialize fiscal years (current year +/- 5 years)
            _fiscalYears = new ObservableCollection<int>();
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear - 5; year <= currentYear + 5; year++)
            {
                _fiscalYears.Add(year);
            }

            RefreshCommand = new AsyncRelayCommand(async () => await LoadDashboardDataAsync());
            ViewAllFundsCommand = new RelayCommand(() => NavigateToFunds());
            ViewAllTransactionsCommand = new RelayCommand(() => NavigateToTransactions());

            // Subscribe to events
            _eventBus.Subscribe<DashboardRefreshEvent>(OnDashboardRefreshRequested);
            _eventBus.Subscribe<FundUpdatedEvent>(OnFundUpdated);
            _eventBus.Subscribe<TransactionCreatedEvent>(OnTransactionCreated);

            InitializeCharts();
        }

        public ObservableCollection<AppropriationFund> Funds { get; }
        public ObservableCollection<Transaction> RecentTransactions { get; }
        public ObservableCollection<AppropriationFund> LowBalanceFunds { get; }
        public ObservableCollection<FundCategorySummary> CategorySummaries { get; }

        public decimal TotalBudget
        {
            get => _totalBudget;
            set => SetProperty(ref _totalBudget, value);
        }

        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        public decimal TotalRemaining
        {
            get => _totalRemaining;
            set => SetProperty(ref _totalRemaining, value);
        }

        public double OverallUtilization
        {
            get => _overallUtilization;
            set => SetProperty(ref _overallUtilization, value);
        }

        public int TotalFunds
        {
            get => _totalFunds;
            set => SetProperty(ref _totalFunds, value);
        }

        public int PendingApprovals
        {
            get => _pendingApprovals;
            set => SetProperty(ref _pendingApprovals, value);
        }

        public ObservableCollection<int> FiscalYears
        {
            get => _fiscalYears;
            set => SetProperty(ref _fiscalYears, value);
        }

        public int CurrentFiscalYear
        {
            get => _currentFiscalYear;
            set
            {
                if (SetProperty(ref _currentFiscalYear, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public ISeries[] BudgetPieChart { get; private set; } = Array.Empty<ISeries>();
        public ISeries[] MonthlyExpensesChart { get; private set; } = Array.Empty<ISeries>();
        public Axis[] MonthlyExpensesXAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] MonthlyExpensesYAxes { get; private set; } = Array.Empty<Axis>();

        public ICommand RefreshCommand { get; }
        public ICommand ViewAllFundsCommand { get; }
        public ICommand ViewAllTransactionsCommand { get; }

        private void InitializeCharts()
        {
            BudgetPieChart = new ISeries[]
            {
                new PieSeries<decimal> { Values = new decimal[] { 0 }, Name = "Utilized" },
                new PieSeries<decimal> { Values = new decimal[] { 0 }, Name = "Remaining" }
            };

            MonthlyExpensesXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" },
                    LabelsRotation = 0
                }
            };

            MonthlyExpensesYAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0")
                }
            };
        }

        public override async Task InitializeAsync()
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Load fund summary
                var summary = await _fundService.GetFundSummaryAsync(CurrentFiscalYear);
                TotalBudget = summary.TotalBudget;
                TotalExpenses = summary.TotalExpenses;
                TotalRemaining = summary.TotalRemaining;
                OverallUtilization = summary.OverallUtilization;
                TotalFunds = summary.FundCount;

                // Load funds
                var funds = await _fundService.GetAllFundsAsync(CurrentFiscalYear);
                Funds.Clear();
                foreach (var fund in funds)
                {
                    Funds.Add(fund);
                }

                // Load low balance funds
                var lowBalanceFunds = await _fundService.GetLowBalanceFundsAsync(CurrentFiscalYear);
                LowBalanceFunds.Clear();
                foreach (var fund in lowBalanceFunds)
                {
                    LowBalanceFunds.Add(fund);
                }

                // Load category summaries
                var categories = await _fundService.GetFundSummaryByCategoryAsync(CurrentFiscalYear);
                CategorySummaries.Clear();
                foreach (var cat in categories)
                {
                    CategorySummaries.Add(cat);
                }

                // Load recent transactions
                var transactions = await _transactionService.GetRecentTransactionsAsync(10);
                RecentTransactions.Clear();
                foreach (var transaction in transactions)
                {
                    RecentTransactions.Add(transaction);
                }

                // Load pending approvals count
                var pendingTransactions = await _transactionService.GetPendingApprovalsAsync();
                PendingApprovals = pendingTransactions.Count;

                // Update charts
                UpdateBudgetPieChart();
                await UpdateMonthlyExpensesChartAsync();

            }, "Loading dashboard data...");
        }

        private void UpdateBudgetPieChart()
        {
            BudgetPieChart = new ISeries[]
            {
                new PieSeries<decimal>
                {
                    Values = new decimal[] { TotalExpenses },
                    Name = "Utilized",
                    Fill = new SolidColorPaint(SKColor.Parse("#dc3545"))
                },
                new PieSeries<decimal>
                {
                    Values = new decimal[] { TotalRemaining },
                    Name = "Remaining",
                    Fill = new SolidColorPaint(SKColor.Parse("#28a745"))
                }
            };
            OnPropertyChanged(nameof(BudgetPieChart));
        }

        private async Task UpdateMonthlyExpensesChartAsync()
        {
            var monthlyData = new decimal[12];

            foreach (var fund in Funds)
            {
                var monthlySummary = await _transactionService.GetMonthlyTransactionSummaryAsync(fund.Id, CurrentFiscalYear);
                for (int i = 0; i < 12; i++)
                {
                    var monthData = monthlySummary.FirstOrDefault(m => m.Month == i + 1);
                    if (monthData != null)
                    {
                        monthlyData[i] += monthData.TotalAmount;
                    }
                }
            }

            MonthlyExpensesChart = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Values = monthlyData,
                    Name = "Monthly Expenses",
                    Fill = new SolidColorPaint(SKColor.Parse("#007bff"))
                }
            };
            OnPropertyChanged(nameof(MonthlyExpensesChart));
        }

        private void NavigateToFunds()
        {
            _eventBus.Publish(new NavigationEvent { ViewName = "Funds" });
        }

        private void NavigateToTransactions()
        {
            _eventBus.Publish(new NavigationEvent { ViewName = "Transactions" });
        }

        private void OnDashboardRefreshRequested(DashboardRefreshEvent evt)
        {
            _ = LoadDashboardDataAsync();
        }

        private void OnFundUpdated(FundUpdatedEvent evt)
        {
            _ = LoadDashboardDataAsync();
        }

        private void OnTransactionCreated(TransactionCreatedEvent evt)
        {
            _ = LoadDashboardDataAsync();
        }

        public override void Cleanup()
        {
            _eventBus.Unsubscribe<DashboardRefreshEvent>(OnDashboardRefreshRequested);
            _eventBus.Unsubscribe<FundUpdatedEvent>(OnFundUpdated);
            _eventBus.Unsubscribe<TransactionCreatedEvent>(OnTransactionCreated);
            base.Cleanup();
        }
    }
}
