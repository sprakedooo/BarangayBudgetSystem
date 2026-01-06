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
        // Fund operations
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
        Task<AppropriationFund?> GetFundWithParticularsAsync(int fundId);

        // FundParticular operations
        Task<List<FundParticular>> GetParticularsForFundAsync(int fundId);
        Task<FundParticular?> GetParticularByIdAsync(int id);
        Task<FundParticular> CreateParticularAsync(FundParticular particular);
        Task<FundParticular> UpdateParticularAsync(FundParticular particular);
        Task<bool> DeleteParticularAsync(int id);
        Task<string> GenerateNextParticularCodeAsync(int fundId);
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

            // Load transactions first, then sum client-side (SQLite doesn't support Sum on decimal)
            var transactions = await _context.Transactions
                .Where(t => t.FundId == fundId &&
                           t.TransactionType == TransactionTypes.Expenditure &&
                           (t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.Completed))
                .ToListAsync();

            var utilizedAmount = transactions.Sum(t => t.Amount);

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

        public async Task<AppropriationFund?> GetFundWithParticularsAsync(int fundId)
        {
            return await _context.Funds
                .Include(f => f.Particulars.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(f => f.Id == fundId);
        }

        // FundParticular operations
        public async Task<List<FundParticular>> GetParticularsForFundAsync(int fundId)
        {
            return await _context.FundParticulars
                .Where(p => p.FundId == fundId && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.ParticularName)
                .ToListAsync();
        }

        public async Task<FundParticular?> GetParticularByIdAsync(int id)
        {
            return await _context.FundParticulars
                .Include(p => p.Fund)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<FundParticular> CreateParticularAsync(FundParticular particular)
        {
            particular.CreatedAt = DateTime.Now;
            particular.IsActive = true;

            // Get max sort order and add 1
            var maxOrder = await _context.FundParticulars
                .Where(p => p.FundId == particular.FundId)
                .MaxAsync(p => (int?)p.SortOrder) ?? 0;
            particular.SortOrder = maxOrder + 1;

            _context.FundParticulars.Add(particular);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = particular.FundId,
                UpdateType = UpdateType.Modified
            });

            return particular;
        }

        public async Task<FundParticular> UpdateParticularAsync(FundParticular particular)
        {
            var existing = await _context.FundParticulars.FindAsync(particular.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Particular with ID {particular.Id} not found.");
            }

            existing.ParticularName = particular.ParticularName;
            existing.Description = particular.Description;
            existing.AllocatedAmount = particular.AllocatedAmount;
            existing.UnitOfMeasure = particular.UnitOfMeasure;
            existing.Quantity = particular.Quantity;
            existing.UnitCost = particular.UnitCost;
            existing.SortOrder = particular.SortOrder;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = existing.FundId,
                UpdateType = UpdateType.Modified
            });

            return existing;
        }

        public async Task<bool> DeleteParticularAsync(int id)
        {
            var particular = await _context.FundParticulars.FindAsync(id);
            if (particular == null) return false;

            // Check if particular has transactions
            var hasTransactions = await _context.Transactions.AnyAsync(t => t.FundParticularId == id);
            if (hasTransactions)
            {
                // Soft delete
                particular.IsActive = false;
                particular.UpdatedAt = DateTime.Now;
            }
            else
            {
                // Hard delete
                _context.FundParticulars.Remove(particular);
            }

            await _context.SaveChangesAsync();

            _eventBus.Publish(new FundUpdatedEvent
            {
                FundId = particular.FundId,
                UpdateType = UpdateType.Modified
            });

            return true;
        }

        public async Task<string> GenerateNextParticularCodeAsync(int fundId)
        {
            var fund = await _context.Funds.FindAsync(fundId);
            if (fund == null) return "P-001";

            var pattern = $"{fund.FundCode}-P";
            var count = await _context.FundParticulars
                .Where(p => p.FundId == fundId)
                .CountAsync();

            return $"{pattern}{(count + 1):D3}";
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
