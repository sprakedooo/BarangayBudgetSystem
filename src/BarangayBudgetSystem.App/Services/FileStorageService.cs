using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BarangayBudgetSystem.App.Data;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Services
{
    public interface IFileStorageService
    {
        Task<Attachment> SaveAttachmentAsync(int transactionId, string sourceFilePath, string attachmentType, int? uploadedByUserId = null, string? description = null);
        Task<bool> DeleteAttachmentAsync(int attachmentId);
        Task<string?> GetAttachmentPathAsync(int attachmentId);
        Task<byte[]?> GetAttachmentContentAsync(int attachmentId);
        string GetAttachmentsFolder(int year);
        string GetReportsFolder();
        string GetLogsFolder();
        string GetBackupsFolder();
        Task<bool> FileExistsAsync(string filePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly AppDbContext _context;
        private readonly string _storagePath;

        public FileStorageService(AppDbContext context)
        {
            _context = context;
            _storagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "storage");

            EnsureDirectoriesExist();
        }

        public FileStorageService(AppDbContext context, string storagePath)
        {
            _context = context;
            _storagePath = storagePath;
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(GetAttachmentsFolder(DateTime.Now.Year));
            Directory.CreateDirectory(GetReportsFolder());
            Directory.CreateDirectory(GetLogsFolder());
            Directory.CreateDirectory(GetBackupsFolder());
        }

        public string GetAttachmentsFolder(int year)
        {
            var path = Path.Combine(_storagePath, "attachments", year.ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        public string GetReportsFolder()
        {
            var path = Path.Combine(_storagePath, "reports");
            Directory.CreateDirectory(path);
            return path;
        }

        public string GetLogsFolder()
        {
            var path = Path.Combine(_storagePath, "logs");
            Directory.CreateDirectory(path);
            return path;
        }

        public string GetBackupsFolder()
        {
            var path = Path.Combine(_storagePath, "backups");
            Directory.CreateDirectory(path);
            return path;
        }

        public async Task<Attachment> SaveAttachmentAsync(int transactionId, string sourceFilePath, string attachmentType, int? uploadedByUserId = null, string? description = null)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source file not found.", sourceFilePath);
            }

            var fileInfo = new FileInfo(sourceFilePath);
            var originalFileName = fileInfo.Name;
            var extension = fileInfo.Extension;
            var contentType = GetContentType(extension);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var year = DateTime.Now.Year;
            var destinationFolder = GetAttachmentsFolder(year);
            var destinationPath = Path.Combine(destinationFolder, uniqueFileName);

            // Copy file
            await Task.Run(() => File.Copy(sourceFilePath, destinationPath, overwrite: false));

            // Create attachment record
            var attachment = new Attachment
            {
                TransactionId = transactionId,
                FileName = uniqueFileName,
                OriginalFileName = originalFileName,
                FilePath = destinationPath,
                ContentType = contentType,
                FileSize = fileInfo.Length,
                AttachmentType = attachmentType,
                Description = description,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.Now
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null) return false;

            // Soft delete in database
            attachment.IsDeleted = true;
            attachment.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Optionally delete physical file (commented out for audit trail)
            // if (File.Exists(attachment.FilePath))
            // {
            //     File.Delete(attachment.FilePath);
            // }

            return true;
        }

        public async Task<string?> GetAttachmentPathAsync(int attachmentId)
        {
            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);

            return attachment?.FilePath;
        }

        public async Task<byte[]?> GetAttachmentContentAsync(int attachmentId)
        {
            var path = await GetAttachmentPathAsync(attachmentId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(path);
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            return Task.FromResult(File.Exists(filePath));
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}
