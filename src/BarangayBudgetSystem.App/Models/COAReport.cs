using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarangayBudgetSystem.App.Models
{
    public class COAReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReportNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ReportTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        public int FiscalYear { get; set; }

        public int? Month { get; set; }

        public int? Quarter { get; set; }

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAppropriation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalObligations { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDisbursements { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnobligatedBalance { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = ReportStatus.Draft;

        public int? GeneratedByUserId { get; set; }

        [ForeignKey(nameof(GeneratedByUserId))]
        public virtual User? GeneratedBy { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public virtual ICollection<COAReportDetail> Details { get; set; } = new List<COAReportDetail>();
    }

    public class COAReportDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportId { get; set; }

        [ForeignKey(nameof(ReportId))]
        public virtual COAReport? Report { get; set; }

        [Required]
        public int FundId { get; set; }

        [ForeignKey(nameof(FundId))]
        public virtual AppropriationFund? Fund { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Appropriation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Obligations { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Disbursements { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public double UtilizationRate => Appropriation > 0
            ? (double)(Disbursements / Appropriation) * 100
            : 0;
    }

    public static class ReportTypes
    {
        public const string Monthly = "Monthly";
        public const string Quarterly = "Quarterly";
        public const string Annual = "Annual";
        public const string Special = "Special";

        public static List<string> GetAll() => new()
        {
            Monthly,
            Quarterly,
            Annual,
            Special
        };
    }

    public static class ReportStatus
    {
        public const string Draft = "Draft";
        public const string Generated = "Generated";
        public const string Reviewed = "Reviewed";
        public const string Submitted = "Submitted";
        public const string Archived = "Archived";

        public static List<string> GetAll() => new()
        {
            Draft,
            Generated,
            Reviewed,
            Submitted,
            Archived
        };
    }
}
