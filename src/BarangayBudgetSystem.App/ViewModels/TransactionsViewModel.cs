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
    public class TransactionsViewModel : BaseViewModel
    {
        private readonly ITransactionService _transactionService;
        private readonly IFundService _fundService;
        private readonly IEventBus _eventBus;

        private Transaction? _selectedTransaction;
        private Transaction _editingTransaction = new();
        private bool _isEditing;
        private bool _isNewTransaction;
        private string? _filterStatus;
        private int? _filterFundId;
        private DateTime? _filterStartDate;
        private DateTime? _filterEndDate;
        private string? _searchTerm;

        public TransactionsViewModel(
            ITransactionService transactionService,
            IFundService fundService,
            IEventBus eventBus)
        {
            _transactionService = transactionService;
            _fundService = fundService;
            _eventBus = eventBus;

            Transactions = new ObservableCollection<Transaction>();
            Funds = new ObservableCollection<AppropriationFund>();
            StatusOptions = new ObservableCollection<string>(TransactionStatus.GetAll());
            TransactionTypeOptions = new ObservableCollection<string>(TransactionTypes.GetAll());

            // Commands
            LoadTransactionsCommand = new AsyncRelayCommand(LoadTransactionsAsync);
            NewTransactionCommand = new RelayCommand(NewTransaction);
            EditTransactionCommand = new RelayCommand<Transaction>(EditTransaction);
            SaveTransactionCommand = new AsyncRelayCommand(SaveTransactionAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            DeleteTransactionCommand = new AsyncRelayCommand<Transaction>(DeleteTransactionAsync);
            ApproveTransactionCommand = new AsyncRelayCommand<Transaction>(ApproveTransactionAsync);
            RejectTransactionCommand = new AsyncRelayCommand<Transaction>(RejectTransactionAsync);
            SubmitForApprovalCommand = new AsyncRelayCommand<Transaction>(SubmitForApprovalAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            SearchCommand = new AsyncRelayCommand(LoadTransactionsAsync);

            // Subscribe to events
            _eventBus.Subscribe<TransactionStatusChangedEvent>(OnTransactionStatusChanged);
        }

        public ObservableCollection<Transaction> Transactions { get; }
        public ObservableCollection<AppropriationFund> Funds { get; }
        public ObservableCollection<string> StatusOptions { get; }
        public ObservableCollection<string> TransactionTypeOptions { get; }

        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        public Transaction EditingTransaction
        {
            get => _editingTransaction;
            set => SetProperty(ref _editingTransaction, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsNewTransaction
        {
            get => _isNewTransaction;
            set => SetProperty(ref _isNewTransaction, value);
        }

        public string? FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                    _ = LoadTransactionsAsync();
            }
        }

        public int? FilterFundId
        {
            get => _filterFundId;
            set
            {
                if (SetProperty(ref _filterFundId, value))
                    _ = LoadTransactionsAsync();
            }
        }

        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                if (SetProperty(ref _filterStartDate, value))
                    _ = LoadTransactionsAsync();
            }
        }

        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                if (SetProperty(ref _filterEndDate, value))
                    _ = LoadTransactionsAsync();
            }
        }

        public string? SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public ICommand LoadTransactionsCommand { get; }
        public ICommand NewTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand SaveTransactionCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand ApproveTransactionCommand { get; }
        public ICommand RejectTransactionCommand { get; }
        public ICommand SubmitForApprovalCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand SearchCommand { get; }

        public override async Task InitializeAsync()
        {
            await LoadFundsAsync();
            await LoadTransactionsAsync();
        }

        private async Task LoadFundsAsync()
        {
            var funds = await _fundService.GetAllFundsAsync(DateTime.Now.Year);
            Funds.Clear();
            foreach (var fund in funds)
            {
                Funds.Add(fund);
            }
        }

        private async Task LoadTransactionsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var filter = new TransactionFilter
                {
                    Status = FilterStatus,
                    FundId = FilterFundId,
                    StartDate = FilterStartDate,
                    EndDate = FilterEndDate,
                    SearchTerm = SearchTerm
                };

                var transactions = await _transactionService.GetAllTransactionsAsync(filter);
                Transactions.Clear();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }
            }, "Loading transactions...");
        }

        private void NewTransaction()
        {
            EditingTransaction = new Transaction
            {
                TransactionDate = DateTime.Today,
                TransactionType = TransactionTypes.Expenditure,
                Status = TransactionStatus.Pending
            };
            IsNewTransaction = true;
            IsEditing = true;
        }

        private void EditTransaction(Transaction? transaction)
        {
            if (transaction == null) return;

            if (transaction.Status != TransactionStatus.Pending)
            {
                ShowWarning("Only pending transactions can be edited.");
                return;
            }

            EditingTransaction = new Transaction
            {
                Id = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                FundId = transaction.FundId,
                TransactionType = transaction.TransactionType,
                Description = transaction.Description,
                Payee = transaction.Payee,
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate,
                PRNumber = transaction.PRNumber,
                PONumber = transaction.PONumber,
                DVNumber = transaction.DVNumber,
                CheckNumber = transaction.CheckNumber,
                CheckDate = transaction.CheckDate,
                Remarks = transaction.Remarks,
                Status = transaction.Status
            };
            IsNewTransaction = false;
            IsEditing = true;
        }

        private async Task SaveTransactionAsync()
        {
            if (EditingTransaction.FundId == 0)
            {
                ShowWarning("Please select a fund.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingTransaction.Description))
            {
                ShowWarning("Please enter a description.");
                return;
            }

            if (EditingTransaction.Amount <= 0)
            {
                ShowWarning("Amount must be greater than zero.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                if (IsNewTransaction)
                {
                    await _transactionService.CreateTransactionAsync(EditingTransaction);
                    ShowMessage("Transaction created successfully.");
                }
                else
                {
                    await _transactionService.UpdateTransactionAsync(EditingTransaction);
                    ShowMessage("Transaction updated successfully.");
                }

                IsEditing = false;
                await LoadTransactionsAsync();
            }, "Saving transaction...");
        }

        private void CancelEdit()
        {
            IsEditing = false;
            EditingTransaction = new Transaction();
        }

        private async Task DeleteTransactionAsync(Transaction? transaction)
        {
            if (transaction == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete transaction {transaction.TransactionNumber}?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _transactionService.DeleteTransactionAsync(transaction.Id);
                ShowMessage("Transaction deleted successfully.");
                await LoadTransactionsAsync();
            }, "Deleting transaction...");
        }

        private async Task ApproveTransactionAsync(Transaction? transaction)
        {
            if (transaction == null) return;

            if (!ShowConfirmation($"Are you sure you want to approve transaction {transaction.TransactionNumber}?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _transactionService.UpdateTransactionStatusAsync(transaction.Id, TransactionStatus.Approved);
                ShowMessage("Transaction approved successfully.");
                await LoadTransactionsAsync();
            }, "Approving transaction...");
        }

        private async Task RejectTransactionAsync(Transaction? transaction)
        {
            if (transaction == null) return;

            if (!ShowConfirmation($"Are you sure you want to reject transaction {transaction.TransactionNumber}?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _transactionService.UpdateTransactionStatusAsync(transaction.Id, TransactionStatus.Rejected);
                ShowMessage("Transaction rejected.");
                await LoadTransactionsAsync();
            }, "Rejecting transaction...");
        }

        private async Task SubmitForApprovalAsync(Transaction? transaction)
        {
            if (transaction == null) return;

            await ExecuteAsync(async () =>
            {
                await _transactionService.UpdateTransactionStatusAsync(transaction.Id, TransactionStatus.ForApproval);
                ShowMessage("Transaction submitted for approval.");
                await LoadTransactionsAsync();
            }, "Submitting transaction...");
        }

        private void ClearFilters()
        {
            FilterStatus = null;
            FilterFundId = null;
            FilterStartDate = null;
            FilterEndDate = null;
            SearchTerm = null;
        }

        private void OnTransactionStatusChanged(TransactionStatusChangedEvent evt)
        {
            _ = LoadTransactionsAsync();
        }

        public override void Cleanup()
        {
            _eventBus.Unsubscribe<TransactionStatusChangedEvent>(OnTransactionStatusChanged);
            base.Cleanup();
        }
    }
}
