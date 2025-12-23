using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Data;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Helpers;

namespace BarangayBudgetSystem.App.Services
{
    public interface ITransactionService
    {
        Task<List<Transaction>> GetAllTransactionsAsync(TransactionFilter? filter = null);
        Task<Transaction?> GetTransactionByIdAsync(int id);
        Task<Transaction?> GetTransactionByNumberAsync(string transactionNumber);
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<Transaction> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(int id);
        Task<Transaction> UpdateTransactionStatusAsync(int id, string newStatus, int? approvedByUserId = null);
        Task<List<Transaction>> GetPendingApprovalsAsync();
        Task<List<Transaction>> GetRecentTransactionsAsync(int count = 10);
        Task<TransactionStatistics> GetTransactionStatisticsAsync(int year);
        Task<string> GenerateNextTransactionNumberAsync();
        Task<string> GenerateNextPRNumberAsync();
        Task<string> GenerateNextPONumberAsync();
        Task<string> GenerateNextDVNumberAsync();
        Task<List<TransactionMonthlySummary>> GetMonthlyTransactionSummaryAsync(int fundId, int year);
    }

    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly IFundService _fundService;

        public TransactionService(AppDbContext context, IEventBus eventBus, IFundService fundService)
        {
            _context = context;
            _eventBus = eventBus;
            _fundService = fundService;
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync(TransactionFilter? filter = null)
        {
            var query = _context.Transactions
                .Include(t => t.Fund)
                .Include(t => t.CreatedBy)
                .Include(t => t.ApprovedBy)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(t => t.Status == filter.Status);

                if (filter.FundId.HasValue)
                    query = query.Where(t => t.FundId == filter.FundId.Value);

                if (!string.IsNullOrEmpty(filter.TransactionType))
                    query = query.Where(t => t.TransactionType == filter.TransactionType);

                if (filter.StartDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.ToLower();
                    query = query.Where(t =>
                        t.TransactionNumber.ToLower().Contains(term) ||
                        t.Description.ToLower().Contains(term) ||
                        (t.Payee != null && t.Payee.ToLower().Contains(term)) ||
                        (t.PRNumber != null && t.PRNumber.ToLower().Contains(term)) ||
                        (t.PONumber != null && t.PONumber.ToLower().Contains(term)) ||
                        (t.DVNumber != null && t.DVNumber.ToLower().Contains(term)));
                }
            }

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Fund)
                .Include(t => t.CreatedBy)
                .Include(t => t.ApprovedBy)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction?> GetTransactionByNumberAsync(string transactionNumber)
        {
            return await _context.Transactions
                .Include(t => t.Fund)
                .FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            // Validate fund balance for expenditures
            if (transaction.TransactionType == TransactionTypes.Expenditure)
            {
                var fund = await _fundService.GetFundByIdAsync(transaction.FundId);
                if (fund == null)
                {
                    throw new InvalidOperationException("Fund not found.");
                }

                if (fund.RemainingBalance < transaction.Amount)
                {
                    throw new InvalidOperationException($"Insufficient fund balance. Available: {fund.RemainingBalance:N2}");
                }
            }

            transaction.TransactionNumber = await GenerateNextTransactionNumberAsync();
            transaction.CreatedAt = DateTime.Now;
            transaction.Status = TransactionStatus.Pending;

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new TransactionCreatedEvent
            {
                TransactionId = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                FundId = transaction.FundId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType
            });

            return transaction;
        }

        public async Task<Transaction> UpdateTransactionAsync(Transaction transaction)
        {
            var existingTransaction = await _context.Transactions.FindAsync(transaction.Id);
            if (existingTransaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transaction.Id} not found.");
            }

            if (existingTransaction.Status != TransactionStatus.Pending)
            {
                throw new InvalidOperationException("Only pending transactions can be modified.");
            }

            existingTransaction.FundId = transaction.FundId;
            existingTransaction.TransactionType = transaction.TransactionType;
            existingTransaction.Description = transaction.Description;
            existingTransaction.Payee = transaction.Payee;
            existingTransaction.Amount = transaction.Amount;
            existingTransaction.TransactionDate = transaction.TransactionDate;
            existingTransaction.PRNumber = transaction.PRNumber;
            existingTransaction.PONumber = transaction.PONumber;
            existingTransaction.DVNumber = transaction.DVNumber;
            existingTransaction.CheckNumber = transaction.CheckNumber;
            existingTransaction.CheckDate = transaction.CheckDate;
            existingTransaction.Remarks = transaction.Remarks;
            existingTransaction.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return existingTransaction;
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return false;

            if (transaction.Status != TransactionStatus.Pending && transaction.Status != TransactionStatus.Rejected)
            {
                throw new InvalidOperationException("Only pending or rejected transactions can be deleted.");
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Transaction> UpdateTransactionStatusAsync(int id, string newStatus, int? approvedByUserId = null)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {id} not found.");
            }

            var oldStatus = transaction.Status;
            transaction.Status = newStatus;
            transaction.UpdatedAt = DateTime.Now;

            if (newStatus == TransactionStatus.Approved || newStatus == TransactionStatus.Completed)
            {
                transaction.ApprovedByUserId = approvedByUserId;
                transaction.ApprovedAt = DateTime.Now;

                // Update fund utilization for expenditures
                if (transaction.TransactionType == TransactionTypes.Expenditure)
                {
                    await _fundService.UpdateFundUtilizationAsync(transaction.FundId);
                }
            }

            await _context.SaveChangesAsync();

            _eventBus.Publish(new TransactionStatusChangedEvent
            {
                TransactionId = id,
                TransactionNumber = transaction.TransactionNumber,
                OldStatus = oldStatus,
                NewStatus = newStatus
            });

            _eventBus.Publish(new DashboardRefreshEvent
            {
                RefreshFunds = true,
                RefreshTransactions = true,
                RefreshCharts = true
            });

            return transaction;
        }

        public async Task<List<Transaction>> GetPendingApprovalsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Fund)
                .Include(t => t.CreatedBy)
                .Where(t => t.Status == TransactionStatus.ForApproval)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetRecentTransactionsAsync(int count = 10)
        {
            return await _context.Transactions
                .Include(t => t.Fund)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<TransactionStatistics> GetTransactionStatisticsAsync(int year)
        {
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate.Year == year)
                .ToListAsync();

            return new TransactionStatistics
            {
                Year = year,
                TotalTransactions = transactions.Count,
                PendingCount = transactions.Count(t => t.Status == TransactionStatus.Pending),
                ForApprovalCount = transactions.Count(t => t.Status == TransactionStatus.ForApproval),
                ApprovedCount = transactions.Count(t => t.Status == TransactionStatus.Approved),
                CompletedCount = transactions.Count(t => t.Status == TransactionStatus.Completed),
                RejectedCount = transactions.Count(t => t.Status == TransactionStatus.Rejected),
                TotalExpenditures = transactions
                    .Where(t => t.TransactionType == TransactionTypes.Expenditure &&
                               (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.Completed))
                    .Sum(t => t.Amount)
            };
        }

        public async Task<string> GenerateNextTransactionNumberAsync()
        {
            var prefix = $"TXN-{DateTime.Now:yyyyMM}-";
            var lastTransaction = await _context.Transactions
                .Where(t => t.TransactionNumber.StartsWith(prefix))
                .OrderByDescending(t => t.TransactionNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastTransaction != null)
            {
                var numberPart = lastTransaction.TransactionNumber.Replace(prefix, "");
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateNextPRNumberAsync()
        {
            var prefix = $"PR-{DateTime.Now:yyyy}-";
            var lastPR = await _context.Transactions
                .Where(t => t.PRNumber != null && t.PRNumber.StartsWith(prefix))
                .OrderByDescending(t => t.PRNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPR?.PRNumber != null)
            {
                var numberPart = lastPR.PRNumber.Replace(prefix, "");
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateNextPONumberAsync()
        {
            var prefix = $"PO-{DateTime.Now:yyyy}-";
            var lastPO = await _context.Transactions
                .Where(t => t.PONumber != null && t.PONumber.StartsWith(prefix))
                .OrderByDescending(t => t.PONumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPO?.PONumber != null)
            {
                var numberPart = lastPO.PONumber.Replace(prefix, "");
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<string> GenerateNextDVNumberAsync()
        {
            var prefix = $"DV-{DateTime.Now:yyyy}-";
            var lastDV = await _context.Transactions
                .Where(t => t.DVNumber != null && t.DVNumber.StartsWith(prefix))
                .OrderByDescending(t => t.DVNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastDV?.DVNumber != null)
            {
                var numberPart = lastDV.DVNumber.Replace(prefix, "");
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<List<TransactionMonthlySummary>> GetMonthlyTransactionSummaryAsync(int fundId, int year)
        {
            var transactions = await _context.Transactions
                .Where(t => t.FundId == fundId &&
                           t.TransactionDate.Year == year &&
                           (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.Completed))
                .ToListAsync();

            return Enumerable.Range(1, 12)
                .Select(month => new TransactionMonthlySummary
                {
                    Month = month,
                    MonthName = new DateTime(year, month, 1).ToString("MMMM"),
                    TransactionCount = transactions.Count(t => t.TransactionDate.Month == month),
                    TotalAmount = transactions
                        .Where(t => t.TransactionDate.Month == month &&
                                   t.TransactionType == TransactionTypes.Expenditure)
                        .Sum(t => t.Amount)
                })
                .ToList();
        }
    }

    public class TransactionFilter
    {
        public string? Status { get; set; }
        public int? FundId { get; set; }
        public string? TransactionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class TransactionStatistics
    {
        public int Year { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingCount { get; set; }
        public int ForApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal TotalExpenditures { get; set; }
    }

    public class TransactionMonthlySummary
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
