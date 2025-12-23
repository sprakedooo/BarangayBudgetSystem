using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<AppropriationFund> Funds { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<COAReport> COAReports { get; set; }
        public DbSet<COAReportDetail> COAReportDetails { get; set; }
        public DbSet<User> Users { get; set; }

        private readonly string _connectionString;

        public AppDbContext()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BarangayBudgetSystem");

            Directory.CreateDirectory(appDataPath);
            _connectionString = $"Data Source={Path.Combine(appDataPath, "budget.db")}";
        }

        public AppDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            _connectionString = string.Empty;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AppropriationFund configuration
            modelBuilder.Entity<AppropriationFund>(entity =>
            {
                entity.HasIndex(e => e.FundCode).IsUnique();
                entity.HasIndex(e => e.FiscalYear);
                entity.HasIndex(e => e.Category);

                entity.Property(e => e.AllocatedAmount).HasDefaultValue(0);
                entity.Property(e => e.UtilizedAmount).HasDefaultValue(0);
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.TransactionNumber).IsUnique();
                entity.HasIndex(e => e.TransactionDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PRNumber);
                entity.HasIndex(e => e.PONumber);
                entity.HasIndex(e => e.DVNumber);

                entity.HasOne(t => t.Fund)
                    .WithMany(f => f.Transactions)
                    .HasForeignKey(t => t.FundId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTransactions)
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.ApprovedBy)
                    .WithMany(u => u.ApprovedTransactions)
                    .HasForeignKey(t => t.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Attachment configuration
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasIndex(e => e.TransactionId);
                entity.HasIndex(e => e.AttachmentType);

                entity.HasOne(a => a.Transaction)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.UploadedBy)
                    .WithMany(u => u.UploadedAttachments)
                    .HasForeignKey(a => a.UploadedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // COAReport configuration
            modelBuilder.Entity<COAReport>(entity =>
            {
                entity.HasIndex(e => e.ReportNumber).IsUnique();
                entity.HasIndex(e => e.FiscalYear);
                entity.HasIndex(e => e.ReportType);

                entity.HasOne(r => r.GeneratedBy)
                    .WithMany(u => u.GeneratedReports)
                    .HasForeignKey(r => r.GeneratedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // COAReportDetail configuration
            modelBuilder.Entity<COAReportDetail>(entity =>
            {
                entity.HasOne(d => d.Report)
                    .WithMany(r => r.Details)
                    .HasForeignKey(d => d.ReportId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Fund)
                    .WithMany()
                    .HasForeignKey(d => d.FundId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email);
            });

            // Seed default admin user
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed default admin user (password: admin123)
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "AQAAAAIAAYagAAAAELqNlJxE8C5KxMJnxqf8DvwSp8VnYwKpq8JKz9qXQSK/tQ==",
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRoles.Administrator,
                Position = "System Administrator",
                IsActive = true,
                CreatedAt = seedDate
            });

            // Seed sample appropriation funds for fiscal year 2025
            // Note: Using fixed year for seed data consistency
            int fiscalYear = 2025;
            modelBuilder.Entity<AppropriationFund>().HasData(
                new AppropriationFund
                {
                    Id = 1,
                    FundCode = $"GF-{fiscalYear}-001",
                    FundName = "General Fund - Personnel Services",
                    Description = "Salaries and wages of barangay officials and employees",
                    AllocatedAmount = 500000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.PersonnelServices,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 2,
                    FundCode = $"GF-{fiscalYear}-002",
                    FundName = "General Fund - MOOE",
                    Description = "Maintenance and Other Operating Expenses",
                    AllocatedAmount = 300000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.MOOE,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 3,
                    FundCode = $"SK-{fiscalYear}-001",
                    FundName = "SK Fund",
                    Description = "Sangguniang Kabataan Fund for youth programs",
                    AllocatedAmount = 100000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.SKFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 4,
                    FundCode = $"DEV-{fiscalYear}-001",
                    FundName = "Barangay Development Fund",
                    Description = "Fund for barangay infrastructure and development projects",
                    AllocatedAmount = 750000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.DevelopmentFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 5,
                    FundCode = $"DF-{fiscalYear}-001",
                    FundName = "Disaster Risk Reduction Fund",
                    Description = "Fund for disaster preparedness and response",
                    AllocatedAmount = 200000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.DisasterFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 6,
                    FundCode = $"GF-{fiscalYear}-003",
                    FundName = "General Fund - Capital Outlay",
                    Description = "Capital expenditures for equipment and infrastructure",
                    AllocatedAmount = 400000.00m,
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.CapitalOutlay,
                    IsActive = true,
                    CreatedAt = seedDate
                }
            );
        }

        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }

        public void Migrate()
        {
            Database.Migrate();
        }
    }
}
