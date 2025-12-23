using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarangayBudgetSystem.App.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionNumber { get; set; } = string.Empty;

        [Required]
        public int FundId { get; set; }

        [ForeignKey(nameof(FundId))]
        public virtual AppropriationFund? Fund { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Payee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = TransactionStatus.Pending;

        [MaxLength(100)]
        public string? PRNumber { get; set; }

        [MaxLength(100)]
        public string? PONumber { get; set; }

        [MaxLength(100)]
        public string? DVNumber { get; set; }

        [MaxLength(100)]
        public string? CheckNumber { get; set; }

        public DateTime? CheckDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User? CreatedBy { get; set; }

        public int? ApprovedByUserId { get; set; }

        [ForeignKey(nameof(ApprovedByUserId))]
        public virtual User? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    public static class TransactionTypes
    {
        public const string Expenditure = "Expenditure";
        public const string Appropriation = "Appropriation";
        public const string Adjustment = "Adjustment";
        public const string Transfer = "Transfer";
        public const string Reversal = "Reversal";

        public static List<string> GetAll() => new()
        {
            Expenditure,
            Appropriation,
            Adjustment,
            Transfer,
            Reversal
        };
    }

    public static class TransactionStatus
    {
        public const string Pending = "Pending";
        public const string ForApproval = "For Approval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";

        public static List<string> GetAll() => new()
        {
            Pending,
            ForApproval,
            Approved,
            Rejected,
            Cancelled,
            Completed
        };
    }
}
