using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarangayBudgetSystem.App.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = UserRoles.Viewer;

        [MaxLength(200)]
        public string? Position { get; set; }

        [MaxLength(200)]
        public string? Department { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Transaction> CreatedTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Transaction> ApprovedTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
        public virtual ICollection<COAReport> GeneratedReports { get; set; } = new List<COAReport>();
    }

    public static class UserRoles
    {
        public const string Administrator = "Administrator";
        public const string Treasurer = "Treasurer";
        public const string Accountant = "Accountant";
        public const string BudgetOfficer = "Budget Officer";
        public const string Encoder = "Encoder";
        public const string Viewer = "Viewer";

        public static List<string> GetAll() => new()
        {
            Administrator,
            Treasurer,
            Accountant,
            BudgetOfficer,
            Encoder,
            Viewer
        };

        public static bool CanApproveTransactions(string role)
        {
            return role == Administrator || role == Treasurer || role == BudgetOfficer;
        }

        public static bool CanCreateTransactions(string role)
        {
            return role != Viewer;
        }

        public static bool CanGenerateReports(string role)
        {
            return role == Administrator || role == Treasurer || role == Accountant || role == BudgetOfficer;
        }

        public static bool CanManageUsers(string role)
        {
            return role == Administrator;
        }

        public static bool CanManageFunds(string role)
        {
            return role == Administrator || role == Treasurer || role == BudgetOfficer;
        }
    }
}
