using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarangayBudgetSystem.App.Models
{
    public class Attachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [ForeignKey(nameof(TransactionId))]
        public virtual Transaction? Transaction { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string AttachmentType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? UploadedByUserId { get; set; }

        [ForeignKey(nameof(UploadedByUserId))]
        public virtual User? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }

    public static class AttachmentTypes
    {
        public const string PurchaseRequest = "Purchase Request";
        public const string PurchaseOrder = "Purchase Order";
        public const string DisbursementVoucher = "Disbursement Voucher";
        public const string Receipt = "Receipt";
        public const string Invoice = "Invoice";
        public const string SupportingDocument = "Supporting Document";
        public const string COAReport = "COA Report";
        public const string Other = "Other";

        public static List<string> GetAll() => new()
        {
            PurchaseRequest,
            PurchaseOrder,
            DisbursementVoucher,
            Receipt,
            Invoice,
            SupportingDocument,
            COAReport,
            Other
        };
    }
}
