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
    public interface IFundService
    {
        Task<List<AppropriationFund>> GetAllFundsAsync(int? fiscalYear = null);
        Task<AppropriationFund?> GetFundByIdAsync(int id);
        Task<AppropriationFund?> GetFundByCodeAsync(string fundCode);
        Task<List<AppropriationFund>> GetFundsByCategoryAsync(string category, int fiscalYear);
        Task<AppropriationFund> CreateFundAsync(AppropriationFund fund);
        Task<AppropriationFund> UpdateFundAsync(AppropriationFund fund);
        Task<bool> DeleteFundAsync(int id);
        Task UpdateFundUtilizationAsync(int fundId);
        Task<FundSummary> GetFundSummaryAsync(int fiscalYear);
        Task<List<FundCategorySummary>> GetFundSummaryByCategoryAsync(int fiscalYear);
        Task<List<AppropriationFund>> GetLowBalanceFundsAsync(int fiscalYear, double threshold = 20);
        Task<string> GenerateNextFundCodeAsync(string category, int fiscalYear);
    }

    public class FundService : IFundService
    {
        private readonly AppDbContext _context;
        private readonly IEventBus _eventBus;

        public FundService(AppDbContext context, IEventBus eventBus)
        {
            _context = context;
            _eventBus = eventBus;
        }

        public async Task<List<AppropriationFund>> GetAllFundsAsync(int? fiscalYear = null)
        {
            var query = _context.Funds.Where(f => f.IsActive);

            if (fiscalYear.HasValue)
            {
                query = query.Where(f => f.FiscalYear == fiscalYear.Value);
            }

            return await query
                .OrderBy(f => f.Category)
                .ThenBy(f => f.FundName)
                .ToListAsync();
        }

        public async Task<AppropriationFund?> GetFundByIdAsync(int id)
        {
            return await _context.Funds
                .Include(f => f.Transactions)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<AppropriationFund?> GetFundByCodeAsync(string fundCode)
        {
            return await _context.Funds
                .FirstOrDefaultAsync(f => f.FundCode == fundCode && f.IsActive);
        }

        public async Task<List<AppropriationFund>> GetFundsByCategoryAsync(string category, int fiscalYear)
        {
            return await _context.Funds
                .Where(f => f.Category == category && f.FiscalYear == fiscalYear && f.IsActive)
                .OrderBy(f => f.FundName)
                .ToListAsync();
        }

        public async Task<AppropriationFund> CreateFundAsync(AppropriationFund fund)
        {
            fund.CreatedAt = DateTime.Now;
            fund.IsActive = true;

            _context.Funds.Add(fund);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = fund.Id,
                FundCode = fund.FundCode,
                NewBalance = fund.RemainingBalance,
                UpdateType = UpdateType.Created
            });

            return fund;
        }

        public async Task<AppropriationFund> UpdateFundAsync(AppropriationFund fund)
        {
            var existingFund = await _context.Funds.FindAsync(fund.Id);
            if (existingFund == null)
            {
                throw new InvalidOperationException($"Fund with ID {fund.Id} not found.");
            }

            existingFund.FundName = fund.FundName;
            existingFund.Description = fund.Description;
            existingFund.AllocatedAmount = fund.AllocatedAmount;
            existingFund.Category = fund.Category;
            existingFund.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = existingFund.Id,
                FundCode = existingFund.FundCode,
                NewBalance = existingFund.RemainingBalance,
                UpdateType = UpdateType.Modified
            });

            return existingFund;
        }

        public async Task<bool> DeleteFundAsync(int id)
        {
            var fund = await _context.Funds.FindAsync(id);
            if (fund == null) return false;

            // Check if fund has transactions
            var hasTransactions = await _context.Transactions.AnyAsync(t => t.FundId == id);
            if (hasTransactions)
            {
                // Soft delete
                fund.IsActive = false;
                fund.UpdatedAt = DateTime.Now;
            }
            else
            {
                // Hard delete
                _context.Funds.Remove(fund);
            }

            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = id,
                FundCode = fund.FundCode,
                UpdateType = UpdateType.Deleted
            });

            return true;
        }

        public async Task UpdateFundUtilizationAsync(int fundId)
        {
            var fund = await _context.Funds.FindAsync(fundId);
            if (fund == null) return;

            var utilizedAmount = await _context.Transactions
                .Where(t => t.FundId == fundId &&
                           t.TransactionType == TransactionTypes.Expenditure &&
                           (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.Completed))
                .SumAsync(t => t.Amount);

            fund.UtilizedAmount = utilizedAmount;
            fund.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = fundId,
                FundCode = fund.FundCode,
                NewBalance = fund.RemainingBalance,
                UpdateType = UpdateType.Modified
            });
        }

        public async Task<FundSummary> GetFundSummaryAsync(int fiscalYear)
        {
            var funds = await _context.Funds
                .Where(f => f.FiscalYear == fiscalYear && f.IsActive)
                .ToListAsync();

            return new FundSummary
            {
                FiscalYear = fiscalYear,
                TotalBudget = funds.Sum(f => f.AllocatedAmount),
                TotalExpenses = funds.Sum(f => f.UtilizedAmount),
                TotalRemaining = funds.Sum(f => f.RemainingBalance),
                FundCount = funds.Count,
                OverallUtilization = funds.Sum(f => f.AllocatedAmount) > 0
                    ? (double)(funds.Sum(f => f.UtilizedAmount) / funds.Sum(f => f.AllocatedAmount)) * 100
                    : 0
            };
        }

        public async Task<List<FundCategorySummary>> GetFundSummaryByCategoryAsync(int fiscalYear)
        {
            // Load data first, then aggregate on client side (SQLite doesn't support Sum on decimal)
            var funds = await _context.Funds
                .Where(f => f.FiscalYear == fiscalYear && f.IsActive)
                .ToListAsync();

            return funds
                .GroupBy(f => f.Category)
                .Select(g => new FundCategorySummary
                {
                    Category = g.Key,
                    FundCount = g.Count(),
                    TotalAllocated = g.Sum(f => f.AllocatedAmount),
                    TotalUtilized = g.Sum(f => f.UtilizedAmount),
                    TotalRemaining = g.Sum(f => f.AllocatedAmount - f.UtilizedAmount)
                })
                .OrderBy(s => s.Category)
                .ToList();
        }

        public async Task<List<AppropriationFund>> GetLowBalanceFundsAsync(int fiscalYear, double threshold = 20)
        {
            var funds = await _context.Funds
                .Where(f => f.FiscalYear == fiscalYear && f.IsActive && f.AllocatedAmount > 0)
                .ToListAsync();

            return funds
                .Where(f => (double)(f.RemainingBalance / f.AllocatedAmount) * 100 < threshold)
                .OrderBy(f => (double)(f.RemainingBalance / f.AllocatedAmount))
                .ToList();
        }

        public async Task<string> GenerateNextFundCodeAsync(string category, int fiscalYear)
        {
            var prefix = category switch
            {
                FundCategories.GeneralFund => "GF",
                FundCategories.SpecialEducationFund => "SEF",
                FundCategories.TrustFund => "TF",
                FundCategories.SKFund => "SK",
                FundCategories.DisasterFund => "DF",
                FundCategories.DevelopmentFund => "DEV",
                FundCategories.PersonnelServices => "PS",
                FundCategories.MOOE => "MOOE",
                FundCategories.CapitalOutlay => "CO",
                _ => "OTH"
            };

            var pattern = $"{prefix}-{fiscalYear}-";
            var lastCode = await _context.Funds
                .Where(f => f.FundCode.StartsWith(pattern))
                .OrderByDescending(f => f.FundCode)
                .Select(f => f.FundCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCode != null)
            {
                var numberPart = lastCode.Replace(pattern, "");
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{pattern}{nextNumber:D3}";
        }
    }

    public class FundSummary
    {
        public int FiscalYear { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRemaining { get; set; }
        public int FundCount { get; set; }
        public double OverallUtilization { get; set; }
    }

    public class FundCategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int FundCount { get; set; }
        public decimal TotalAllocated { get; set; }
        public decimal TotalUtilized { get; set; }
        public decimal TotalRemaining { get; set; }
        public double UtilizationPercentage => TotalAllocated > 0
            ? (double)(TotalUtilized / TotalAllocated) * 100
            : 0;
    }
}
