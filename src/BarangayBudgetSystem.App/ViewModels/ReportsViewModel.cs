using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Services;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IReportGenerationService _reportService;
        private readonly IDocumentService _documentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IComparativeReportExportService _comparativeReportService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly IEventBus _eventBus;

        private COAReport? _selectedReport;
        private int _selectedFiscalYear;
        private int _selectedMonth;
        private int _selectedQuarter;
        private string? _filterReportType;
        private BudgetUtilizationReport? _budgetUtilizationReport;
        private CashFlowReport? _cashFlowReport;

        public ReportsViewModel(
            IReportGenerationService reportService,
            IDocumentService documentService,
            IFileStorageService fileStorageService,
            IComparativeReportExportService comparativeReportService,
            IAppSettingsService appSettingsService,
            IEventBus eventBus)
        {
            _reportService = reportService;
            _documentService = documentService;
            _fileStorageService = fileStorageService;
            _comparativeReportService = comparativeReportService;
            _appSettingsService = appSettingsService;
            _eventBus = eventBus;

            _selectedFiscalYear = DateTime.Now.Year;
            _selectedMonth = DateTime.Now.Month;
            _selectedQuarter = (DateTime.Now.Month - 1) / 3 + 1;

            Reports = new ObservableCollection<COAReport>();
            ReportTypeOptions = new ObservableCollection<string>(ReportTypes.GetAll());
            FiscalYears = new ObservableCollection<int>();
            Months = new ObservableCollection<MonthItem>();
            Quarters = new ObservableCollection<QuarterItem>();

            InitializeSelectionLists();

            // Commands
            LoadReportsCommand = new AsyncRelayCommand(LoadReportsAsync);
            GenerateMonthlyReportCommand = new AsyncRelayCommand(GenerateMonthlyReportAsync);
            GenerateQuarterlyReportCommand = new AsyncRelayCommand(GenerateQuarterlyReportAsync);
            GenerateAnnualReportCommand = new AsyncRelayCommand(GenerateAnnualReportAsync);
            ViewReportCommand = new AsyncRelayCommand<COAReport>(ViewReportAsync);
            ExportReportCommand = new AsyncRelayCommand<COAReport>(ExportReportAsync);
            DeleteReportCommand = new AsyncRelayCommand<COAReport>(DeleteReportAsync);
            SubmitReportCommand = new AsyncRelayCommand<COAReport>(SubmitReportAsync);
            GenerateBudgetUtilizationCommand = new AsyncRelayCommand(GenerateBudgetUtilizationAsync);
            GenerateCashFlowCommand = new AsyncRelayCommand(GenerateCashFlowAsync);
            ExportComparativeStatementCommand = new AsyncRelayCommand(ExportComparativeStatementAsync);

            // Subscribe to events
            _eventBus.Subscribe<ReportGeneratedEvent>(OnReportGenerated);
        }

        public ObservableCollection<COAReport> Reports { get; }
        public ObservableCollection<string> ReportTypeOptions { get; }
        public ObservableCollection<int> FiscalYears { get; }
        public ObservableCollection<MonthItem> Months { get; }
        public ObservableCollection<QuarterItem> Quarters { get; }

        public COAReport? SelectedReport
        {
            get => _selectedReport;
            set => SetProperty(ref _selectedReport, value);
        }

        public int SelectedFiscalYear
        {
            get => _selectedFiscalYear;
            set
            {
                if (SetProperty(ref _selectedFiscalYear, value))
                    _ = LoadReportsAsync();
            }
        }

        public int SelectedMonth
        {
            get => _selectedMonth;
            set => SetProperty(ref _selectedMonth, value);
        }

        public int SelectedQuarter
        {
            get => _selectedQuarter;
            set => SetProperty(ref _selectedQuarter, value);
        }

        public string? FilterReportType
        {
            get => _filterReportType;
            set
            {
                if (SetProperty(ref _filterReportType, value))
                    _ = LoadReportsAsync();
            }
        }

        public BudgetUtilizationReport? BudgetUtilizationReport
        {
            get => _budgetUtilizationReport;
            set => SetProperty(ref _budgetUtilizationReport, value);
        }

        public CashFlowReport? CashFlowReport
        {
            get => _cashFlowReport;
            set => SetProperty(ref _cashFlowReport, value);
        }

        public ICommand LoadReportsCommand { get; }
        public ICommand GenerateMonthlyReportCommand { get; }
        public ICommand GenerateQuarterlyReportCommand { get; }
        public ICommand GenerateAnnualReportCommand { get; }
        public ICommand ViewReportCommand { get; }
        public ICommand ExportReportCommand { get; }
        public ICommand DeleteReportCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand GenerateBudgetUtilizationCommand { get; }
        public ICommand GenerateCashFlowCommand { get; }
        public ICommand ExportComparativeStatementCommand { get; }

        private void InitializeSelectionLists()
        {
            // Fiscal years
            var currentYear = DateTime.Now.Year;
            for (int year = currentYear - 5; year <= currentYear + 1; year++)
            {
                FiscalYears.Add(year);
            }

            // Months
            for (int month = 1; month <= 12; month++)
            {
                Months.Add(new MonthItem
                {
                    Month = month,
                    Name = new DateTime(2024, month, 1).ToString("MMMM")
                });
            }

            // Quarters
            Quarters.Add(new QuarterItem { Quarter = 1, Name = "Q1 (Jan-Mar)" });
            Quarters.Add(new QuarterItem { Quarter = 2, Name = "Q2 (Apr-Jun)" });
            Quarters.Add(new QuarterItem { Quarter = 3, Name = "Q3 (Jul-Sep)" });
            Quarters.Add(new QuarterItem { Quarter = 4, Name = "Q4 (Oct-Dec)" });
        }

        public override async Task InitializeAsync()
        {
            await LoadReportsAsync();
        }

        private async Task LoadReportsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var reports = await _reportService.GetReportsAsync(SelectedFiscalYear, FilterReportType);
                Reports.Clear();
                foreach (var report in reports)
                {
                    Reports.Add(report);
                }
            }, "Loading reports...");
        }

        private async Task GenerateMonthlyReportAsync()
        {
            await ExecuteAsync(async () =>
            {
                var report = await _reportService.GenerateMonthlyReportAsync(SelectedFiscalYear, SelectedMonth);
                ShowMessage($"Monthly report generated successfully.\nReport Number: {report.ReportNumber}");
                await LoadReportsAsync();
            }, "Generating monthly report...");
        }

        private async Task GenerateQuarterlyReportAsync()
        {
            await ExecuteAsync(async () =>
            {
                var report = await _reportService.GenerateQuarterlyReportAsync(SelectedFiscalYear, SelectedQuarter);
                ShowMessage($"Quarterly report generated successfully.\nReport Number: {report.ReportNumber}");
                await LoadReportsAsync();
            }, "Generating quarterly report...");
        }

        private async Task GenerateAnnualReportAsync()
        {
            await ExecuteAsync(async () =>
            {
                var report = await _reportService.GenerateAnnualReportAsync(SelectedFiscalYear);
                ShowMessage($"Annual report generated successfully.\nReport Number: {report.ReportNumber}");
                await LoadReportsAsync();
            }, "Generating annual report...");
        }

        private async Task ViewReportAsync(COAReport? report)
        {
            if (report == null) return;

            var detailedReport = await _reportService.GetReportByIdAsync(report.Id);
            if (detailedReport != null)
            {
                SelectedReport = detailedReport;
            }
        }

        private async Task ExportReportAsync(COAReport? report)
        {
            if (report == null) return;

            await ExecuteAsync(async () =>
            {
                var outputPath = _fileStorageService.GetReportsFolder();
                var filePath = await _documentService.GenerateCOAReportDocumentAsync(
                    report, "Barangay Sample", outputPath);

                if (ShowConfirmation($"Report exported to:\n{filePath}\n\nWould you like to open it?"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }, "Exporting report...");
        }

        private async Task DeleteReportAsync(COAReport? report)
        {
            if (report == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete report {report.ReportNumber}?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _reportService.DeleteReportAsync(report.Id);
                ShowMessage("Report deleted successfully.");
                await LoadReportsAsync();
            }, "Deleting report...");
        }

        private async Task SubmitReportAsync(COAReport? report)
        {
            if (report == null) return;

            if (!ShowConfirmation($"Are you sure you want to submit report {report.ReportNumber} to COA?\n\nThis action cannot be undone."))
                return;

            await ExecuteAsync(async () =>
            {
                await _reportService.UpdateReportStatusAsync(report.Id, ReportStatus.Submitted);
                ShowMessage("Report submitted successfully.");
                await LoadReportsAsync();
            }, "Submitting report...");
        }

        private async Task GenerateBudgetUtilizationAsync()
        {
            await ExecuteAsync(async () =>
            {
                BudgetUtilizationReport = await _reportService.GenerateBudgetUtilizationReportAsync(SelectedFiscalYear);
            }, "Generating budget utilization report...");
        }

        private async Task GenerateCashFlowAsync()
        {
            await ExecuteAsync(async () =>
            {
                CashFlowReport = await _reportService.GenerateCashFlowReportAsync(SelectedFiscalYear);
            }, "Generating cash flow report...");
        }

        private async Task ExportComparativeStatementAsync()
        {
            await ExecuteAsync(async () =>
            {
                var outputPath = _fileStorageService.GetReportsFolder();
                var provinceName = _appSettingsService.Settings.ProvinceName ?? "Sample Province";
                var barangayName = _appSettingsService.Settings.BarangayName ?? "Sample Barangay";
                var asOfDate = new DateTime(SelectedFiscalYear, SelectedMonth, 1);

                var filePath = await _comparativeReportService.ExportComparativeStatementAsync(
                    SelectedFiscalYear,
                    outputPath,
                    provinceName,
                    barangayName,
                    asOfDate);

                if (ShowConfirmation($"Comparative Statement exported to:\n{filePath}\n\nWould you like to open it?"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }, "Exporting comparative statement...");
        }

        private void OnReportGenerated(ReportGeneratedEvent evt)
        {
            _ = LoadReportsAsync();
        }

        public override void Cleanup()
        {
            _eventBus.Unsubscribe<ReportGeneratedEvent>(OnReportGenerated);
            base.Cleanup();
        }
    }

    public class MonthItem
    {
        public int Month { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class QuarterItem
    {
        public int Quarter { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
