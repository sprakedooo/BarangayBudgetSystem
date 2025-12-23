using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Data;
using BarangayBudgetSystem.App.Models;
using BarangayBudgetSystem.App.Helpers;

namespace BarangayBudgetSystem.App.Services
{
    public interface IReportGenerationService
    {
        Task<COAReport> GenerateMonthlyReportAsync(int fiscalYear, int month, int? generatedByUserId = null);
        Task<COAReport> GenerateQuarterlyReportAsync(int fiscalYear, int quarter, int? generatedByUserId = null);
        Task<COAReport> GenerateAnnualReportAsync(int fiscalYear, int? generatedByUserId = null);
        Task<List<COAReport>> GetReportsAsync(int fiscalYear, string? reportType = null);
        Task<COAReport?> GetReportByIdAsync(int id);
        Task<COAReport> UpdateReportStatusAsync(int id, string newStatus);
        Task<bool> DeleteReportAsync(int id);
        Task<BudgetUtilizationReport> GenerateBudgetUtilizationReportAsync(int fiscalYear);
        Task<CashFlowReport> GenerateCashFlowReportAsync(int fiscalYear);
    }

    public class ReportGenerationService : IReportGenerationService
    {
        private readonly AppDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly string _reportsPath;

        public ReportGenerationService(AppDbContext context, IEventBus eventBus)
        {
            _context = context;
            _eventBus = eventBus;
            _reportsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "storage", "reports");
            Directory.CreateDirectory(_reportsPath);
        }

        public async Task<COAReport> GenerateMonthlyReportAsync(int fiscalYear, int month, int? generatedByUserId = null)
        {
            var periodStart = new DateTime(fiscalYear, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var report = new COAReport
            {
                ReportNumber = $"COA-{fiscalYear}-{month:D2}-{DateTime.Now:yyyyMMddHHmmss}",
                ReportTitle = $"Monthly Budget Utilization Report - {periodStart:MMMM yyyy}",
                ReportType = ReportTypes.Monthly,
                FiscalYear = fiscalYear,
                Month = month,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.Now,
                Status = ReportStatus.Generated
            };

            await PopulateReportDetailsAsync(report, periodStart, periodEnd);

            _context.COAReports.Add(report);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new ReportGeneratedEvent
            {
                ReportId = report.Id,
                ReportNumber = report.ReportNumber,
                ReportType = report.ReportType,
                FilePath = report.FilePath
            });

            return report;
        }

        public async Task<COAReport> GenerateQuarterlyReportAsync(int fiscalYear, int quarter, int? generatedByUserId = null)
        {
            var startMonth = (quarter - 1) * 3 + 1;
            var periodStart = new DateTime(fiscalYear, startMonth, 1);
            var periodEnd = periodStart.AddMonths(3).AddDays(-1);

            var report = new COAReport
            {
                ReportNumber = $"COA-{fiscalYear}-Q{quarter}-{DateTime.Now:yyyyMMddHHmmss}",
                ReportTitle = $"Quarterly Budget Utilization Report - Q{quarter} {fiscalYear}",
                ReportType = ReportTypes.Quarterly,
                FiscalYear = fiscalYear,
                Quarter = quarter,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.Now,
                Status = ReportStatus.Generated
            };

            await PopulateReportDetailsAsync(report, periodStart, periodEnd);

            _context.COAReports.Add(report);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new ReportGeneratedEvent
            {
                ReportId = report.Id,
                ReportNumber = report.ReportNumber,
                ReportType = report.ReportType,
                FilePath = report.FilePath
            });

            return report;
        }

        public async Task<COAReport> GenerateAnnualReportAsync(int fiscalYear, int? generatedByUserId = null)
        {
            var periodStart = new DateTime(fiscalYear, 1, 1);
            var periodEnd = new DateTime(fiscalYear, 12, 31);

            var report = new COAReport
            {
                ReportNumber = $"COA-{fiscalYear}-ANNUAL-{DateTime.Now:yyyyMMddHHmmss}",
                ReportTitle = $"Annual Budget Execution Report - {fiscalYear}",
                ReportType = ReportTypes.Annual,
                FiscalYear = fiscalYear,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.Now,
                Status = ReportStatus.Generated
            };

            await PopulateReportDetailsAsync(report, periodStart, periodEnd);

            _context.COAReports.Add(report);
            await _context.SaveChangesAsync();

            _eventBus.Publish(new ReportGeneratedEvent
            {
                ReportId = report.Id,
                ReportNumber = report.ReportNumber,
                ReportType = report.ReportType,
                FilePath = report.FilePath
            });

            return report;
        }

        private async Task PopulateReportDetailsAsync(COAReport report, DateTime periodStart, DateTime periodEnd)
        {
            var funds = await _context.Funds
                .Where(f => f.FiscalYear == report.FiscalYear && f.IsActive)
                .ToListAsync();

            foreach (var fund in funds)
            {
                var transactions = await _context.Transactions
                    .Where(t => t.FundId == fund.Id &&
                               t.TransactionDate >= periodStart &&
                               t.TransactionDate <= periodEnd &&
                               t.TransactionType == TransactionTypes.Expenditure)
                    .ToListAsync();

                var obligations = transactions
                    .Where(t => t.Status == TransactionStatus.Approved || t.Status == TransactionStatus.Completed)
                    .Sum(t => t.Amount);

                var disbursements = transactions
                    .Where(t => t.Status == TransactionStatus.Completed)
                    .Sum(t => t.Amount);

                var detail = new COAReportDetail
                {
                    FundId = fund.Id,
                    Appropriation = fund.AllocatedAmount,
                    Obligations = obligations,
                    Disbursements = disbursements,
                    Balance = fund.AllocatedAmount - obligations
                };

                report.Details.Add(detail);
            }

            report.TotalAppropriation = report.Details.Sum(d => d.Appropriation);
            report.TotalObligations = report.Details.Sum(d => d.Obligations);
            report.TotalDisbursements = report.Details.Sum(d => d.Disbursements);
            report.UnobligatedBalance = report.TotalAppropriation - report.TotalObligations;
        }

        public async Task<List<COAReport>> GetReportsAsync(int fiscalYear, string? reportType = null)
        {
            var query = _context.COAReports
                .Include(r => r.GeneratedBy)
                .Where(r => r.FiscalYear == fiscalYear);

            if (!string.IsNullOrEmpty(reportType))
            {
                query = query.Where(r => r.ReportType == reportType);
            }

            return await query
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync();
        }

        public async Task<COAReport?> GetReportByIdAsync(int id)
        {
            return await _context.COAReports
                .Include(r => r.GeneratedBy)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Fund)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<COAReport> UpdateReportStatusAsync(int id, string newStatus)
        {
            var report = await _context.COAReports.FindAsync(id);
            if (report == null)
            {
                throw new InvalidOperationException($"Report with ID {id} not found.");
            }

            report.Status = newStatus;
            if (newStatus == ReportStatus.Submitted)
            {
                report.SubmittedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<bool> DeleteReportAsync(int id)
        {
            var report = await _context.COAReports
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return false;

            if (report.Status == ReportStatus.Submitted)
            {
                throw new InvalidOperationException("Submitted reports cannot be deleted.");
            }

            _context.COAReportDetails.RemoveRange(report.Details);
            _context.COAReports.Remove(report);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<BudgetUtilizationReport> GenerateBudgetUtilizationReportAsync(int fiscalYear)
        {
            var funds = await _context.Funds
                .Where(f => f.FiscalYear == fiscalYear && f.IsActive)
                .ToListAsync();

            var report = new BudgetUtilizationReport
            {
                FiscalYear = fiscalYear,
                GeneratedAt = DateTime.Now,
                TotalAppropriation = funds.Sum(f => f.AllocatedAmount),
                TotalUtilized = funds.Sum(f => f.UtilizedAmount),
                TotalRemaining = funds.Sum(f => f.RemainingBalance)
            };

            report.Items = funds.Select(f => new BudgetUtilizationItem
            {
                FundCode = f.FundCode,
                FundName = f.FundName,
                Category = f.Category,
                Appropriation = f.AllocatedAmount,
                Utilized = f.UtilizedAmount,
                Remaining = f.RemainingBalance,
                UtilizationRate = f.UtilizationPercentage
            }).ToList();

            report.CategorySummaries = funds
                .GroupBy(f => f.Category)
                .Select(g => new CategoryUtilizationSummary
                {
                    Category = g.Key,
                    Appropriation = g.Sum(f => f.AllocatedAmount),
                    Utilized = g.Sum(f => f.UtilizedAmount),
                    Remaining = g.Sum(f => f.RemainingBalance)
                }).ToList();

            return report;
        }

        public async Task<CashFlowReport> GenerateCashFlowReportAsync(int fiscalYear)
        {
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate.Year == fiscalYear)
                .ToListAsync();

            var report = new CashFlowReport
            {
                FiscalYear = fiscalYear,
                GeneratedAt = DateTime.Now
            };

            report.MonthlyFlows = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var monthTransactions = transactions.Where(t => t.TransactionDate.Month == month).ToList();
                    return new MonthlyCashFlow
                    {
                        Month = month,
                        MonthName = new DateTime(fiscalYear, month, 1).ToString("MMMM"),
                        Inflows = monthTransactions
                            .Where(t => t.TransactionType == TransactionTypes.Appropriation)
                            .Sum(t => t.Amount),
                        Outflows = monthTransactions
                            .Where(t => t.TransactionType == TransactionTypes.Expenditure &&
                                       t.Status == TransactionStatus.Completed)
                            .Sum(t => t.Amount)
                    };
                }).ToList();

            report.TotalInflows = report.MonthlyFlows.Sum(m => m.Inflows);
            report.TotalOutflows = report.MonthlyFlows.Sum(m => m.Outflows);
            report.NetFlow = report.TotalInflows - report.TotalOutflows;

            return report;
        }
    }

    public class BudgetUtilizationReport
    {
        public int FiscalYear { get; set; }
        public DateTime GeneratedAt { get; set; }
        public decimal TotalAppropriation { get; set; }
        public decimal TotalUtilized { get; set; }
        public decimal TotalRemaining { get; set; }
        public double OverallUtilizationRate => TotalAppropriation > 0
            ? (double)(TotalUtilized / TotalAppropriation) * 100 : 0;
        public List<BudgetUtilizationItem> Items { get; set; } = new();
        public List<CategoryUtilizationSummary> CategorySummaries { get; set; } = new();
    }

    public class BudgetUtilizationItem
    {
        public string FundCode { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Appropriation { get; set; }
        public decimal Utilized { get; set; }
        public decimal Remaining { get; set; }
        public double UtilizationRate { get; set; }
    }

    public class CategoryUtilizationSummary
    {
        public string Category { get; set; } = string.Empty;
        public decimal Appropriation { get; set; }
        public decimal Utilized { get; set; }
        public decimal Remaining { get; set; }
        public double UtilizationRate => Appropriation > 0
            ? (double)(Utilized / Appropriation) * 100 : 0;
    }

    public class CashFlowReport
    {
        public int FiscalYear { get; set; }
        public DateTime GeneratedAt { get; set; }
        public decimal TotalInflows { get; set; }
        public decimal TotalOutflows { get; set; }
        public decimal NetFlow { get; set; }
        public List<MonthlyCashFlow> MonthlyFlows { get; set; } = new();
    }

    public class MonthlyCashFlow
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Inflows { get; set; }
        public decimal Outflows { get; set; }
        public decimal NetFlow => Inflows - Outflows;
    }
}
