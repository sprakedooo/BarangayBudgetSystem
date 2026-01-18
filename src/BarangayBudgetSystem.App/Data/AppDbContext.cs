using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<FiscalYearBudget> FiscalYearBudgets { get; set; }
        public DbSet<AppropriationFund> Funds { get; set; }
        public DbSet<FundParticular> FundParticulars { get; set; }
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

            // FiscalYearBudget configuration
            modelBuilder.Entity<FiscalYearBudget>(entity =>
            {
                entity.HasIndex(e => e.FiscalYear).IsUnique();
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.NTAAmount).HasDefaultValue(0);
                entity.Property(e => e.EstimatedLocalIncome).HasDefaultValue(0);
                entity.Property(e => e.OtherIncome).HasDefaultValue(0);
            });

            // AppropriationFund configuration
            modelBuilder.Entity<AppropriationFund>(entity =>
            {
                entity.HasIndex(e => e.FundCode).IsUnique();
                entity.HasIndex(e => e.FiscalYear);
                entity.HasIndex(e => e.Category);

                entity.Property(e => e.AllocatedAmount).HasDefaultValue(0);
                entity.Property(e => e.UtilizedAmount).HasDefaultValue(0);

                entity.HasOne(f => f.FiscalYearBudget)
                    .WithMany(b => b.Funds)
                    .HasForeignKey(f => f.FiscalYearBudgetId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FundParticular configuration
            modelBuilder.Entity<FundParticular>(entity =>
            {
                entity.HasIndex(e => e.ParticularCode);
                entity.HasIndex(e => e.FundId);

                entity.Property(e => e.AllocatedAmount).HasDefaultValue(0);
                entity.Property(e => e.UtilizedAmount).HasDefaultValue(0);

                entity.HasOne(p => p.Fund)
                    .WithMany(f => f.Particulars)
                    .HasForeignKey(p => p.FundId)
                    .OnDelete(DeleteBehavior.Cascade);
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

                entity.HasOne(t => t.FundParticular)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(t => t.FundParticularId)
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
            // Hash is SHA256("admin123" + "BarangayBudgetSalt2024") encoded as Base64
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "Gy0+zblQw1tLz8G1e2hT8jMKGjFzKz3n5F0BqRd5Bkk=",
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRoles.Administrator,
                Position = "System Administrator",
                IsActive = true,
                CreatedAt = seedDate
            });

            // Seed sample appropriation funds for fiscal year 2025
            // Based on Philippine Barangay Budget Structure
            // Assuming total IRA of P4,000,000 for demonstration
            int fiscalYear = 2025;
            modelBuilder.Entity<AppropriationFund>().HasData(
                new AppropriationFund
                {
                    Id = 1,
                    FundCode = $"PS-{fiscalYear}-001",
                    FundName = "Personal Services",
                    Description = "Salaries, wages, honoraria and benefits of barangay officials and employees",
                    AllocatedAmount = 1200000.00m,  // 30% of IRA
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.PersonnelServices,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 2,
                    FundCode = $"MOOE-{fiscalYear}-001",
                    FundName = "Maintenance and Other Operating Expenses",
                    Description = "Office supplies, utilities, travel, repairs and maintenance",
                    AllocatedAmount = 800000.00m,  // 20% of IRA
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.MOOE,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 3,
                    FundCode = $"CO-{fiscalYear}-001",
                    FundName = "Capital Outlay",
                    Description = "Equipment, furniture, fixtures, and infrastructure projects",
                    AllocatedAmount = 400000.00m,  // 10% of IRA
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.CapitalOutlay,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 4,
                    FundCode = $"DEV-{fiscalYear}-001",
                    FundName = "20% Development Fund",
                    Description = "Mandated 20% allocation for development projects (RA 7160)",
                    AllocatedAmount = 800000.00m,  // 20% of IRA (mandated)
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.DevelopmentFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 5,
                    FundCode = $"DRRM-{fiscalYear}-001",
                    FundName = "5% DRRM Fund",
                    Description = "Mandated 5% for Disaster Risk Reduction and Management (RA 10121)",
                    AllocatedAmount = 200000.00m,  // 5% of IRA (mandated)
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.DRRMFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 6,
                    FundCode = $"GAD-{fiscalYear}-001",
                    FundName = "Gender and Development Fund",
                    Description = "At least 5% for Gender and Development programs (RA 9710)",
                    AllocatedAmount = 200000.00m,  // 5% of IRA (mandated)
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.GADFund,
                    IsActive = true,
                    CreatedAt = seedDate
                },
                new AppropriationFund
                {
                    Id = 7,
                    FundCode = $"SK-{fiscalYear}-001",
                    FundName = "Sangguniang Kabataan Fund",
                    Description = "10% allocation for SK programs and youth development (RA 10742)",
                    AllocatedAmount = 400000.00m,  // 10% of IRA (mandated)
                    UtilizedAmount = 0,
                    FiscalYear = fiscalYear,
                    Category = FundCategories.SKFund,
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
