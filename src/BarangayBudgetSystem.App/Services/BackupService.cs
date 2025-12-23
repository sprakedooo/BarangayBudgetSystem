using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BarangayBudgetSystem.App.Services
{
    public interface IBackupService
    {
        Task<string> CreateBackupAsync(string? customName = null);
        Task<bool> RestoreBackupAsync(string backupFilePath);
        Task<List<BackupInfo>> GetAvailableBackupsAsync();
        Task<bool> DeleteBackupAsync(string backupFilePath);
        Task CleanupOldBackupsAsync(int keepCount = 10);
        string GetBackupFolder();
    }

    public class BackupService : IBackupService
    {
        private readonly string _backupPath;
        private readonly string _databasePath;
        private readonly string _attachmentsPath;

        public BackupService()
        {
            var basePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..", "storage");

            _backupPath = Path.Combine(basePath, "backups");
            _attachmentsPath = Path.Combine(basePath, "attachments");

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BarangayBudgetSystem");
            _databasePath = Path.Combine(appDataPath, "budget.db");

            Directory.CreateDirectory(_backupPath);
        }

        public BackupService(string backupPath, string databasePath, string attachmentsPath)
        {
            _backupPath = backupPath;
            _databasePath = databasePath;
            _attachmentsPath = attachmentsPath;

            Directory.CreateDirectory(_backupPath);
        }

        public string GetBackupFolder() => _backupPath;

        public async Task<string> CreateBackupAsync(string? customName = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupName = string.IsNullOrEmpty(customName)
                ? $"backup_{timestamp}"
                : $"{customName}_{timestamp}";

            var backupFilePath = Path.Combine(_backupPath, $"{backupName}.zip");
            var tempFolder = Path.Combine(_backupPath, $"temp_{timestamp}");

            try
            {
                Directory.CreateDirectory(tempFolder);

                // Copy database
                if (File.Exists(_databasePath))
                {
                    var dbBackupPath = Path.Combine(tempFolder, "budget.db");
                    await Task.Run(() => File.Copy(_databasePath, dbBackupPath, overwrite: true));
                }

                // Copy attachments
                if (Directory.Exists(_attachmentsPath))
                {
                    var attachmentsBackupPath = Path.Combine(tempFolder, "attachments");
                    await Task.Run(() => CopyDirectory(_attachmentsPath, attachmentsBackupPath));
                }

                // Create backup info file
                var infoContent = $"Backup Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"Name: {backupName}\n" +
                                 $"Database: {(File.Exists(_databasePath) ? "Yes" : "No")}\n" +
                                 $"Attachments: {(Directory.Exists(_attachmentsPath) ? "Yes" : "No")}\n";
                await File.WriteAllTextAsync(Path.Combine(tempFolder, "backup_info.txt"), infoContent);

                // Create zip archive
                await Task.Run(() =>
                {
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                    ZipFile.CreateFromDirectory(tempFolder, backupFilePath, CompressionLevel.Optimal, false);
                });

                return backupFilePath;
            }
            finally
            {
                // Cleanup temp folder
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file not found.", backupFilePath);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var tempFolder = Path.Combine(_backupPath, $"restore_temp_{timestamp}");

            try
            {
                // Extract backup
                await Task.Run(() => ZipFile.ExtractToDirectory(backupFilePath, tempFolder));

                // Create pre-restore backup
                await CreateBackupAsync($"pre_restore_{timestamp}");

                // Restore database
                var dbBackupPath = Path.Combine(tempFolder, "budget.db");
                if (File.Exists(dbBackupPath))
                {
                    var dbDir = Path.GetDirectoryName(_databasePath);
                    if (!string.IsNullOrEmpty(dbDir))
                    {
                        Directory.CreateDirectory(dbDir);
                    }
                    await Task.Run(() => File.Copy(dbBackupPath, _databasePath, overwrite: true));
                }

                // Restore attachments
                var attachmentsBackupPath = Path.Combine(tempFolder, "attachments");
                if (Directory.Exists(attachmentsBackupPath))
                {
                    if (Directory.Exists(_attachmentsPath))
                    {
                        Directory.Delete(_attachmentsPath, recursive: true);
                    }
                    await Task.Run(() => CopyDirectory(attachmentsBackupPath, _attachmentsPath));
                }

                return true;
            }
            finally
            {
                // Cleanup temp folder
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
            }
        }

        public async Task<List<BackupInfo>> GetAvailableBackupsAsync()
        {
            return await Task.Run(() =>
            {
                var backups = new List<BackupInfo>();

                if (!Directory.Exists(_backupPath))
                {
                    return backups;
                }

                var zipFiles = Directory.GetFiles(_backupPath, "*.zip");
                foreach (var file in zipFiles)
                {
                    var fileInfo = new FileInfo(file);
                    backups.Add(new BackupInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = file,
                        CreatedAt = fileInfo.CreationTime,
                        FileSize = fileInfo.Length,
                        FileSizeFormatted = FormatFileSize(fileInfo.Length)
                    });
                }

                return backups.OrderByDescending(b => b.CreatedAt).ToList();
            });
        }

        public async Task<bool> DeleteBackupAsync(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                return false;
            }

            await Task.Run(() => File.Delete(backupFilePath));
            return true;
        }

        public async Task CleanupOldBackupsAsync(int keepCount = 10)
        {
            var backups = await GetAvailableBackupsAsync();

            if (backups.Count <= keepCount)
            {
                return;
            }

            var backupsToDelete = backups
                .OrderByDescending(b => b.CreatedAt)
                .Skip(keepCount)
                .ToList();

            foreach (var backup in backupsToDelete)
            {
                await DeleteBackupAsync(backup.FilePath);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:N2} {suffixes[suffixIndex]}";
        }
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
    }
}
