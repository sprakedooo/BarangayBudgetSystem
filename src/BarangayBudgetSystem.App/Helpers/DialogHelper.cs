using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace BarangayBudgetSystem.App.Helpers
{
    public interface IDialogHelper
    {
        void ShowMessage(string message, string title = "Information");
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        bool ShowConfirmation(string message, string title = "Confirm");
        MessageBoxResult ShowQuestion(string message, string title = "Question");
        string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open File");
        string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string title = "Save File", string? defaultFileName = null);
        string? ShowFolderDialog(string description = "Select a folder");
        string[]? ShowOpenMultipleFilesDialog(string filter = "All files (*.*)|*.*", string title = "Open Files");
    }

    public class DialogHelper : IDialogHelper
    {
        public void ShowMessage(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public bool ShowConfirmation(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public MessageBoxResult ShowQuestion(string message, string title = "Question")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }

        public string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open File")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                CheckFileExists = true,
                CheckPathExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string filter = "All files (*.*)|*.*", string title = "Save File", string? defaultFileName = null)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                Title = title,
                FileName = defaultFileName ?? string.Empty,
                OverwritePrompt = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowFolderDialog(string description = "Select a folder")
        {
            var dialog = new OpenFolderDialog
            {
                Title = description
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        public string[]? ShowOpenMultipleFilesDialog(string filter = "All files (*.*)|*.*", string title = "Open Files")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : null;
        }
    }

    public static class DialogFilters
    {
        public const string AllFiles = "All files (*.*)|*.*";
        public const string Documents = "Documents (*.pdf;*.doc;*.docx)|*.pdf;*.doc;*.docx";
        public const string PDFFiles = "PDF files (*.pdf)|*.pdf";
        public const string WordFiles = "Word documents (*.doc;*.docx)|*.doc;*.docx";
        public const string ExcelFiles = "Excel files (*.xls;*.xlsx)|*.xls;*.xlsx";
        public const string ImageFiles = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
        public const string TextFiles = "Text files (*.txt)|*.txt";
        public const string CSVFiles = "CSV files (*.csv)|*.csv";

        public static string Combine(params string[] filters)
        {
            return string.Join("|", filters);
        }
    }
}
