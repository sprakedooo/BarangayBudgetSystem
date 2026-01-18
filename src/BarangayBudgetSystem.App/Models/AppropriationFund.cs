using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace BarangayBudgetSystem.App.Models
{
    public class AppropriationFund
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FundCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FundName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UtilizedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance => AllocatedAmount - UtilizedAmount;

        public double UtilizationPercentage => AllocatedAmount > 0
            ? (double)(UtilizedAmount / AllocatedAmount) * 100
            : 0;

        [Required]
        public int FiscalYear { get; set; }

        /// <summary>
        /// Optional link to the fiscal year budget setup
        /// </summary>
        public int? FiscalYearBudgetId { get; set; }

        [ForeignKey(nameof(FiscalYearBudgetId))]
        public virtual FiscalYearBudget? FiscalYearBudget { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<FundParticular> Particulars { get; set; } = new List<FundParticular>();

        // Computed properties for totals including particulars
        [NotMapped]
        public decimal TotalParticularsAllocated => Particulars?.Sum(p => p.AllocatedAmount) ?? 0;

        [NotMapped]
        public decimal TotalParticularsUtilized => Particulars?.Sum(p => p.UtilizedAmount) ?? 0;

        [NotMapped]
        public bool HasParticulars => Particulars != null && Particulars.Count > 0;
    }

    /// <summary>
    /// Philippine Barangay Budget Categories based on the Local Government Code
    /// and Commission on Audit (COA) guidelines.
    /// </summary>
    public static class FundCategories
    {
        // Main Expenditure Classifications
        public const string PersonnelServices = "Personal Services (PS)";
        public const string MOOE = "MOOE";
        public const string CapitalOutlay = "Capital Outlay (CO)";

        // Mandated Allocations
        public const string DevelopmentFund = "20% Development Fund";
        public const string DRRMFund = "5% DRRM Fund";
        public const string GADFund = "GAD Fund";
        public const string SKFund = "SK Fund";

        // Other Funds
        public const string GeneralFund = "General Fund";
        public const string TrustFund = "Trust Fund";
        public const string SpecialEducationFund = "Special Education Fund";

        /// <summary>
        /// Returns all fund categories in logical order for dropdown display
        /// </summary>
        public static List<string> GetAll() => new()
        {
            PersonnelServices,
            MOOE,
            CapitalOutlay,
            DevelopmentFund,
            DRRMFund,
            GADFund,
            SKFund,
            GeneralFund,
            TrustFund,
            SpecialEducationFund
        };

        /// <summary>
        /// Returns categories that are part of the Annual Investment Plan (AIP)
        /// </summary>
        public static List<string> GetAIPCategories() => new()
        {
            PersonnelServices,
            MOOE,
            CapitalOutlay,
            DevelopmentFund,
            DRRMFund,
            GADFund,
            SKFund
        };

        /// <summary>
        /// Returns the mandated percentage allocation for a category (based on total IRA)
        /// Returns 0 if no mandated percentage exists
        /// </summary>
        public static double GetMandatedPercentage(string category) => category switch
        {
            DevelopmentFund => 20.0,    // 20% for development projects (RA 7160)
            DRRMFund => 5.0,            // 5% for disaster preparedness (RA 10121)
            GADFund => 5.0,             // 5% for gender programs (RA 9710)
            SKFund => 10.0,             // 10% for SK programs (RA 10742)
            _ => 0.0
        };

        /// <summary>
        /// Returns description of the fund category
        /// </summary>
        public static string GetDescription(string category) => category switch
        {
            PersonnelServices => "Salaries, wages, honoraria, and benefits of barangay officials and employees",
            MOOE => "Maintenance and Other Operating Expenses - office supplies, utilities, travel, repairs",
            CapitalOutlay => "Purchase of equipment, furniture, and infrastructure projects",
            DevelopmentFund => "Mandated 20% allocation for development projects (RA 7160)",
            DRRMFund => "Mandated 5% for Disaster Risk Reduction and Management (RA 10121)",
            GADFund => "At least 5% for Gender and Development programs (RA 9710)",
            SKFund => "10% allocation for Sangguniang Kabataan programs (RA 10742)",
            GeneralFund => "General purpose fund for day-to-day operations",
            TrustFund => "Funds held in trust for specific purposes",
            SpecialEducationFund => "Fund for education-related expenses",
            _ => string.Empty
        };

        /// <summary>
        /// Returns the fund code prefix for auto-generation
        /// </summary>
        public static string GetCodePrefix(string category) => category switch
        {
            PersonnelServices => "PS",
            MOOE => "MOOE",
            CapitalOutlay => "CO",
            DevelopmentFund => "DEV",
            DRRMFund => "DRRM",
            GADFund => "GAD",
            SKFund => "SK",
            GeneralFund => "GF",
            TrustFund => "TF",
            SpecialEducationFund => "SEF",
            _ => "OTH"
        };
    }

    /// <summary>
    /// Helper class for calculating budget allocation requirements
    /// </summary>
    public static class BudgetAllocationHelper
    {
        /// <summary>
        /// Calculates required allocation amounts based on total IRA
        /// </summary>
        public static Dictionary<string, decimal> CalculateRequiredAllocations(decimal totalIRA)
        {
            return new Dictionary<string, decimal>
            {
                { FundCategories.DevelopmentFund, totalIRA * 0.20m },
                { FundCategories.DRRMFund, totalIRA * 0.05m },
                { FundCategories.GADFund, totalIRA * 0.05m },
                { FundCategories.SKFund, totalIRA * 0.10m }
            };
        }

        /// <summary>
        /// Validates if allocations meet mandated percentages
        /// </summary>
        public static List<string> ValidateAllocations(decimal totalBudget, Dictionary<string, decimal> allocations)
        {
            var violations = new List<string>();

            foreach (var category in FundCategories.GetAIPCategories())
            {
                var mandatedPct = FundCategories.GetMandatedPercentage(category);
                if (mandatedPct > 0)
                {
                    var requiredAmount = totalBudget * (decimal)(mandatedPct / 100.0);
                    var actualAmount = allocations.GetValueOrDefault(category, 0);

                    if (actualAmount < requiredAmount)
                    {
                        violations.Add($"{category}: Required {requiredAmount:N2} ({mandatedPct}%), Allocated {actualAmount:N2}");
                    }
                }
            }

            return violations;
        }
    }
}
