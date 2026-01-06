using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarangayBudgetSystem.App.Models
{
    /// <summary>
    /// Represents a PPA (Program, Project, Activity) or line item under an appropriation fund
    /// as part of the Annual Investment Plan (AIP).
    ///
    /// Examples:
    /// Under Personnel Services fund:
    /// - Honorarium for Barangay Officials
    /// - Utility Workers
    /// - Barangay Workers
    ///
    /// Under MOOE fund:
    /// - Office Supplies
    /// - Internet Services
    /// - Electricity
    /// - Water
    /// </summary>
    public class FundParticular
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FundId { get; set; }

        [ForeignKey(nameof(FundId))]
        public virtual AppropriationFund? Fund { get; set; }

        [Required]
        [MaxLength(100)]
        public string ParticularCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string ParticularName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UtilizedAmount { get; set; }

        [NotMapped]
        public decimal RemainingBalance => AllocatedAmount - UtilizedAmount;

        [NotMapped]
        public double UtilizationPercentage => AllocatedAmount > 0
            ? (double)(UtilizedAmount / AllocatedAmount) * 100
            : 0;

        [MaxLength(50)]
        public string? UnitOfMeasure { get; set; }

        public int? Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
