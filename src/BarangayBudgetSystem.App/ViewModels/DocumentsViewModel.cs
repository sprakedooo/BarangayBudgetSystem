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
    public class DocumentsViewModel : BaseViewModel
    {
        private readonly ITransactionService _transactionService;
        private readonly IDocumentService _documentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDialogHelper _dialogHelper;
        private readonly IEventBus _eventBus;

        private Transaction? _selectedTransaction;
        private Attachment? _selectedAttachment;
        private string _barangayName = "Barangay Sample";

        public DocumentsViewModel(
            ITransactionService transactionService,
            IDocumentService documentService,
            IFileStorageService fileStorageService,
            IDialogHelper dialogHelper,
            IEventBus eventBus)
        {
            _transactionService = transactionService;
            _documentService = documentService;
            _fileStorageService = fileStorageService;
            _dialogHelper = dialogHelper;
            _eventBus = eventBus;

            Transactions = new ObservableCollection<Transaction>();
            Attachments = new ObservableCollection<Attachment>();
            AttachmentTypeOptions = new ObservableCollection<string>(AttachmentTypes.GetAll());

            // Commands
            LoadTransactionsCommand = new AsyncRelayCommand(LoadTransactionsAsync);
            SelectTransactionCommand = new AsyncRelayCommand<Transaction>(SelectTransactionAsync);
            GeneratePRCommand = new AsyncRelayCommand<Transaction>(GeneratePRAsync);
            GeneratePOCommand = new AsyncRelayCommand<Transaction>(GeneratePOAsync);
            GenerateDVCommand = new AsyncRelayCommand<Transaction>(GenerateDVAsync);
            AddAttachmentCommand = new AsyncRelayCommand(AddAttachmentAsync);
            ViewAttachmentCommand = new RelayCommand<Attachment>(ViewAttachment);
            DeleteAttachmentCommand = new AsyncRelayCommand<Attachment>(DeleteAttachmentAsync);
            OpenFolderCommand = new RelayCommand(OpenDocumentsFolder);
        }

        public ObservableCollection<Transaction> Transactions { get; }
        public ObservableCollection<Attachment> Attachments { get; }
        public ObservableCollection<string> AttachmentTypeOptions { get; }

        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        public Attachment? SelectedAttachment
        {
            get => _selectedAttachment;
            set => SetProperty(ref _selectedAttachment, value);
        }

        public string BarangayName
        {
            get => _barangayName;
            set => SetProperty(ref _barangayName, value);
        }

        public ICommand LoadTransactionsCommand { get; }
        public ICommand SelectTransactionCommand { get; }
        public ICommand GeneratePRCommand { get; }
        public ICommand GeneratePOCommand { get; }
        public ICommand GenerateDVCommand { get; }
        public ICommand AddAttachmentCommand { get; }
        public ICommand ViewAttachmentCommand { get; }
        public ICommand DeleteAttachmentCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public override async Task InitializeAsync()
        {
            await LoadTransactionsAsync();
        }

        private async Task LoadTransactionsAsync()
        {
            await ExecuteAsync(async () =>
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                Transactions.Clear();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }
            }, "Loading transactions...");
        }

        private async Task SelectTransactionAsync(Transaction? transaction)
        {
            if (transaction == null) return;

            await ExecuteAsync(async () =>
            {
                var fullTransaction = await _transactionService.GetTransactionByIdAsync(transaction.Id);
                SelectedTransaction = fullTransaction;

                Attachments.Clear();
                if (fullTransaction?.Attachments != null)
                {
                    foreach (var attachment in fullTransaction.Attachments)
                    {
                        if (!attachment.IsDeleted)
                        {
                            Attachments.Add(attachment);
                        }
                    }
                }
            }, "Loading transaction details...");
        }

        private async Task GeneratePRAsync(Transaction? transaction)
        {
            if (transaction == null)
            {
                ShowWarning("Please select a transaction first.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Generate PR number if not exists
                if (string.IsNullOrEmpty(transaction.PRNumber))
                {
                    transaction.PRNumber = await _transactionService.GenerateNextPRNumberAsync();
                    await _transactionService.UpdateTransactionAsync(transaction);
                }

                var outputPath = _fileStorageService.GetReportsFolder();
                var filePath = await _documentService.GeneratePurchaseRequestAsync(
                    transaction, BarangayName, outputPath);

                if (ShowConfirmation($"Purchase Request generated:\n{filePath}\n\nWould you like to open it?"))
                {
                    OpenFile(filePath);
                }
            }, "Generating Purchase Request...");
        }

        private async Task GeneratePOAsync(Transaction? transaction)
        {
            if (transaction == null)
            {
                ShowWarning("Please select a transaction first.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Generate PO number if not exists
                if (string.IsNullOrEmpty(transaction.PONumber))
                {
                    transaction.PONumber = await _transactionService.GenerateNextPONumberAsync();
                    await _transactionService.UpdateTransactionAsync(transaction);
                }

                var outputPath = _fileStorageService.GetReportsFolder();
                var filePath = await _documentService.GeneratePurchaseOrderAsync(
                    transaction, BarangayName, outputPath);

                if (ShowConfirmation($"Purchase Order generated:\n{filePath}\n\nWould you like to open it?"))
                {
                    OpenFile(filePath);
                }
            }, "Generating Purchase Order...");
        }

        private async Task GenerateDVAsync(Transaction? transaction)
        {
            if (transaction == null)
            {
                ShowWarning("Please select a transaction first.");
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Generate DV number if not exists
                if (string.IsNullOrEmpty(transaction.DVNumber))
                {
                    transaction.DVNumber = await _transactionService.GenerateNextDVNumberAsync();
                    await _transactionService.UpdateTransactionAsync(transaction);
                }

                var outputPath = _fileStorageService.GetReportsFolder();
                var filePath = await _documentService.GenerateDisbursementVoucherAsync(
                    transaction, BarangayName, outputPath);

                if (ShowConfirmation($"Disbursement Voucher generated:\n{filePath}\n\nWould you like to open it?"))
                {
                    OpenFile(filePath);
                }
            }, "Generating Disbursement Voucher...");
        }

        private async Task AddAttachmentAsync()
        {
            if (SelectedTransaction == null)
            {
                ShowWarning("Please select a transaction first.");
                return;
            }

            var filePath = _dialogHelper.ShowOpenFileDialog(
                DialogFilters.Combine(DialogFilters.Documents, DialogFilters.ImageFiles, DialogFilters.AllFiles),
                "Select Attachment");

            if (string.IsNullOrEmpty(filePath)) return;

            await ExecuteAsync(async () =>
            {
                var attachment = await _fileStorageService.SaveAttachmentAsync(
                    SelectedTransaction.Id,
                    filePath,
                    AttachmentTypes.SupportingDocument);

                Attachments.Add(attachment);
                ShowMessage("Attachment added successfully.");
            }, "Adding attachment...");
        }

        private void ViewAttachment(Attachment? attachment)
        {
            if (attachment == null) return;

            if (File.Exists(attachment.FilePath))
            {
                OpenFile(attachment.FilePath);
            }
            else
            {
                ShowWarning("Attachment file not found.");
            }
        }

        private async Task DeleteAttachmentAsync(Attachment? attachment)
        {
            if (attachment == null) return;

            if (!ShowConfirmation($"Are you sure you want to delete attachment {attachment.OriginalFileName}?"))
                return;

            await ExecuteAsync(async () =>
            {
                await _fileStorageService.DeleteAttachmentAsync(attachment.Id);
                Attachments.Remove(attachment);
                ShowMessage("Attachment deleted successfully.");
            }, "Deleting attachment...");
        }

        private void OpenDocumentsFolder()
        {
            var folderPath = _fileStorageService.GetReportsFolder();
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }

        private void OpenFile(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
    }
}
