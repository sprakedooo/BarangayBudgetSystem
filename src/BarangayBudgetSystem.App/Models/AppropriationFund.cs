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

    public static class FundCategories
    {
        public const string GeneralFund = "General Fund";
        public const string SpecialEducationFund = "Special Education Fund";
        public const string TrustFund = "Trust Fund";
        public const string SKFund = "SK Fund";
        public const string DisasterFund = "Disaster Fund";
        public const string DevelopmentFund = "Development Fund";
        public const string PersonnelServices = "Personnel Services";
        public const string MOOE = "MOOE";
        public const string CapitalOutlay = "Capital Outlay";

        public static List<string> GetAll() => new()
        {
            GeneralFund,
            SpecialEducationFund,
            TrustFund,
            SKFund,
            DisasterFund,
            DevelopmentFund,
            PersonnelServices,
            MOOE,
            CapitalOutlay
        };
    }
}
