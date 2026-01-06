using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BarangayBudgetSystem.App.Helpers;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Services;

namespace BarangayBudgetSystem.App.ViewModels
{
    public class FundsViewModel : BaseViewModel
    {
        private readonly IFundService _fundService;
        private readonly IEventBus _eventBus;

        private AppropriationFund? _selectedFund;
        private AppropriationFund _editingFund = new();
        private bool _isEditing;
        private bool _isNewFund;
        private int _currentFiscalYear;
        private string? _filterCategory;

        // Particulars
        private FundParticular? _selectedParticular;
        private FundParticular _editingParticular = new();
        private bool _isEditingParticular;
        private bool _isNewParticular;
        private bool _showParticularsPanel;

        public FundsViewModel(IFundService fundService, IEventBus eventBus)
        {
            _fundService = fundService;
            _eventBus = eventBus;
            _currentFiscalYear = DateTime.Now.Year;

            Funds = new ObservableCollection<AppropriationFund>();
            Particulars = new ObservableCollection<FundParticular>();
            CategoryOptions = new ObservableCollection<string>(FundCategories.GetAll());
            FiscalYears = new ObservableCollection<int>();

            // Populate fiscal years (current year and 5 years back/forward)
            for (int year = _currentFiscalYear - 5; year <= _currentFiscalYear + 5; year++)
            {
                FiscalYears.Add(year);
            }

            // Fund Commands
            LoadFundsCommand = new AsyncRelayCommand(LoadFundsAsync);
            NewFundCommand = new AsyncRelayCommand(NewFundAsync);
            EditFundCommand = new RelayCommand<AppropriationFund>(EditFund);
            SaveFundCommand = new AsyncRelayCommand(SaveFundAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteFundCommand = new AsyncRelayCommand<AppropriationFund>(DeleteFundAsync);
            ViewTransactionsCommand = new RelayCommand<AppropriationFund>(ViewFundTransactions);
            ViewParticularsCommand = new AsyncRelayCommand<AppropriationFund>(ViewParticularsAsync);

            // Particular Commands
            NewParticularCommand = new AsyncRelayCommand(NewParticularAsync);
            EditParticularCommand = new RelayCommand<FundParticular>(EditParticular);
            SaveParticularCommand = new AsyncRelayCommand(SaveParticularAsync);
            CancelParticularEditCommand = new RelayCommand(CancelParticularEdit);
            DeleteParticularCommand = new AsyncRelayCommand<FundParticular>(DeleteParticularAsync);
            CloseParticularsCommand = new RelayCommand(CloseParticulars);

            // Subscribe to events
            _eventBus.Subscribe<FundUpdatedEvent>(OnFundUpdated);
        }

        public ObservableCollection<AppropriationFund> Funds { get; }
        public ObservableCollection<FundParticular> Particulars { get; }
        public ObservableCollection<string> CategoryOptions { get; }
        public ObservableCollection<int> FiscalYears { get; }

        public AppropriationFund? SelectedFund
        {
            get => _selectedFund;
            set => SetProperty(ref _selectedFund, value);
        }

        public AppropriationFund EditingFund
        {
            get => _editingFund;
            set => SetProperty(ref _editingFund, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsNewFund
        {
            get => _isNewFund;
            set => SetProperty(ref _isNewFund, value);
        }

        public int CurrentFiscalYear
        {
            get => _currentFiscalYear;
            set
            {
                if (SetProperty(ref _currentFiscalYear, value))
                    _ = LoadFundsAsync();
            }
        }

        public string? FilterCategory
        {
            get => _filterCategory;
            set
            {
                if (SetProperty(ref _filterCategory, value))
                    _ = LoadFundsAsync();
            }
        }

        // Particular Properties
        public FundParticular? SelectedParticular
        {
            get => _selectedParticular;
            set => SetProperty(ref _selectedParticular, value);
        }

        public FundParticular EditingParticular
        {
            get => _editingParticular;
            set => SetProperty(ref _editingParticular, value);
        }

        public bool IsEditingParticular
        {
            get => _isEditingParticular;
            set => SetProperty(ref _isEditingParticular, value);
        }

        public bool IsNewParticular
        {
            get => _isNewParticular;
            set => SetProperty(ref _isNewParticular, value);
        }

        public bool ShowParticularsPanel
        {
            get => _showParticularsPanel;
            set => SetProperty(ref _showParticularsPanel, value);
        }

        public decimal TotalParticularsAllocated => Particulars.Sum(p => p.AllocatedAmount);
        public decimal RemainingToAllocate => (SelectedFund?.AllocatedAmount ?? 0) - TotalParticularsAllocated;

        // Fund Commands
        public ICommand LoadFundsCommand { get; }
        public ICommand NewFundCommand { get; }
        public ICommand EditFundCommand { get; }
        public ICommand SaveFundCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteFundCommand { get; }
        public ICommand ViewTransactionsCommand { get; }
        public ICommand ViewParticularsCommand { get; }

        // Particular Commands
        public ICommand NewParticularCommand { get; }
        public ICommand EditParticularCommand { get; }
        public ICommand SaveParticularCommand { get; }
        public ICommand CancelParticularEditCommand { get; }
        public ICommand DeleteParticularCommand { get; }
        public ICommand CloseParticularsCommand { get; }

        public override async Task InitializeAsync()
        {
            await LoadFundsAsync();
        }

        private async Task LoadFundsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var funds = await _fundService.GetAllFundsAsync(CurrentFiscalYear);

                if (!string.IsNullOrEmpty(FilterCategory))
                {
                    funds = funds.FindAll(f => f.Category == FilterCategory);
                }

                Funds.Clear();
                foreach (var fund in funds)
                {
                    Funds.Add(fund);
                }
            }, "Loading funds...");
        }

        private async Task NewFundAsync()
        {
            var nextCode = await _fundService.GenerateNextFundCodeAsync(
                FundCategories.GeneralFund, CurrentFiscalYear);

            EditingFund = new AppropriationFund
            {
                FundCode = nextCode,
                FiscalYear = CurrentFiscalYear,
                Category = FundCategories.GeneralFund,
                IsActive = true
            };
            IsNewFund = true;
            IsEditing = true;
        }

        private void EditFund(AppropriationFund? fund)
        {
            if (fund == null) return;

            EditingFund = new AppropriationFund
            {
                Id = fund.Id,
                FundCode = fund.FundCode,
                FundName = fund.FundName,
                Description = fund.Description,
                AllocatedAmount = fund.AllocatedAmount,
                UtilizedAmount = fund.UtilizedAmount,
                FiscalYear = fund.FiscalYear,
                Category = fund.Category,
                IsActive = fund.IsActive
            };
            IsNewFund = false;
            IsEditing = true;
        }

        private async Task SaveFundAsync()
        {
            if (string.IsNullOrWhiteSpace(EditingFund.FundCode))
            {
                ShowWarning("Please enter a fund code.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingFund.FundName))
            {
                ShowWarning("Please enter a fund name.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingFund.Category))
            {
                ShowWarning("Please select a category.");
                return;
            }

            if (EditingFund.AllocatedAmount < 0)
            {
                ShowWarning("Allocated amount cannot be negative.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                if (IsNewFund)
                {
                    await _fundService.CreateFundAsync(EditingFund);
                    ShowMessage("Fund created successfully.");
                }
                else
                {
                    await _fundService.UpdateFundAsync(EditingFund);
                    ShowMessage("Fund updated successfully.");
                }

                IsEditing = false;
                await LoadFundsAsync();
            }, "Saving fund...");
        }

        private void CancelEdit()
        {
            IsEditing = false;
            EditingFund = new AppropriationFund();
        }

        private async Task DeleteFundAsync(AppropriationFund? fund)
        {
            if (fund == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete fund {fund.FundCode} - {fund.FundName}?\n\nNote: Funds with transactions will be deactivated instead of deleted."))
                return;

            await ExecuteAsync(async () =>
            {
                await _fundService.DeleteFundAsync(fund.Id);
                ShowMessage("Fund deleted/deactivated successfully.");
                await LoadFundsAsync();
            }, "Deleting fund...");
        }

        private void ViewFundTransactions(AppropriationFund? fund)
        {
            if (fund == null) return;

            _eventBus.Publish(new NavigationEvent
            {
                ViewName = "Transactions",
                Parameter = new { FundId = fund.Id }
            });
        }

        private void OnFundUpdated(FundUpdatedEvent evt)
        {
            _ = LoadFundsAsync();
        }

        public async Task UpdateFundCodeForCategoryAsync(string category)
        {
            if (IsNewFund && !string.IsNullOrEmpty(category))
            {
                var newCode = await _fundService.GenerateNextFundCodeAsync(category, CurrentFiscalYear);
                EditingFund.FundCode = newCode;
                OnPropertyChanged(nameof(EditingFund));
            }
        }

        // Particular Methods
        private async Task ViewParticularsAsync(AppropriationFund? fund)
        {
            if (fund == null) return;

            SelectedFund = fund;
            await LoadParticularsAsync(fund.Id);
            ShowParticularsPanel = true;
            IsEditing = false;
        }

        private async Task LoadParticularsAsync(int fundId)
        {
            var particulars = await _fundService.GetParticularsForFundAsync(fundId);
            Particulars.Clear();
            foreach (var particular in particulars)
            {
                Particulars.Add(particular);
            }
            OnPropertyChanged(nameof(TotalParticularsAllocated));
            OnPropertyChanged(nameof(RemainingToAllocate));
        }

        private async Task NewParticularAsync()
        {
            if (SelectedFund == null) return;

            var nextCode = await _fundService.GenerateNextParticularCodeAsync(SelectedFund.Id);

            EditingParticular = new FundParticular
            {
                FundId = SelectedFund.Id,
                ParticularCode = nextCode,
                IsActive = true
            };
            IsNewParticular = true;
            IsEditingParticular = true;
        }

        private void EditParticular(FundParticular? particular)
        {
            if (particular == null) return;

            EditingParticular = new FundParticular
            {
                Id = particular.Id,
                FundId = particular.FundId,
                ParticularCode = particular.ParticularCode,
                ParticularName = particular.ParticularName,
                Description = particular.Description,
                AllocatedAmount = particular.AllocatedAmount,
                UtilizedAmount = particular.UtilizedAmount,
                UnitOfMeasure = particular.UnitOfMeasure,
                Quantity = particular.Quantity,
                UnitCost = particular.UnitCost,
                SortOrder = particular.SortOrder,
                IsActive = particular.IsActive
            };
            IsNewParticular = false;
            IsEditingParticular = true;
        }

        private async Task SaveParticularAsync()
        {
            if (string.IsNullOrWhiteSpace(EditingParticular.ParticularName))
            {
                ShowWarning("Please enter a particular name.");
                return;
            }

            if (EditingParticular.AllocatedAmount < 0)
            {
                ShowWarning("Allocated amount cannot be negative.");
                return;
            }

            // Check if allocation exceeds fund amount
            var currentTotal = Particulars.Sum(p => p.AllocatedAmount);
            if (!IsNewParticular)
            {
                var existingParticular = Particulars.FirstOrDefault(p => p.Id == EditingParticular.Id);
                if (existingParticular != null)
                {
                    currentTotal -= existingParticular.AllocatedAmount;
                }
            }

            if (currentTotal + EditingParticular.AllocatedAmount > (SelectedFund?.AllocatedAmount ?? 0))
            {
                ShowWarning("Total particulars allocation cannot exceed the fund's allocated amount.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                if (IsNewParticular)
                {
                    await _fundService.CreateParticularAsync(EditingParticular);
                    ShowMessage("Particular created successfully.");
                }
                else
                {
                    await _fundService.UpdateParticularAsync(EditingParticular);
                    ShowMessage("Particular updated successfully.");
                }

                IsEditingParticular = false;
                if (SelectedFund != null)
                {
                    await LoadParticularsAsync(SelectedFund.Id);
                }
            }, "Saving particular...");
        }

        private void CancelParticularEdit()
        {
            IsEditingParticular = false;
            EditingParticular = new FundParticular();
        }

        private async Task DeleteParticularAsync(FundParticular? particular)
        {
            if (particular == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete '{particular.ParticularName}'?\n\nNote: Particulars with transactions will be deactivated instead of deleted."))
                return;

            await ExecuteAsync(async () =>
            {
                await _fundService.DeleteParticularAsync(particular.Id);
                ShowMessage("Particular deleted/deactivated successfully.");
                if (SelectedFund != null)
                {
                    await LoadParticularsAsync(SelectedFund.Id);
                }
            }, "Deleting particular...");
        }

        private void CloseParticulars()
        {
            ShowParticularsPanel = false;
            SelectedFund = null;
            Particulars.Clear();
            IsEditingParticular = false;
        }

        public override void Cleanup()
        {
            _eventBus.Unsubscribe<FundUpdatedEvent>(OnFundUpdated);
            base.Cleanup();
        }
    }
}
