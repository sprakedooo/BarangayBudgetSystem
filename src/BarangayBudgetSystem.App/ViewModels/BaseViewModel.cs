using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using BarangayBudgetSystem.App.Helpers;

namespace BarangayBudgetSystem.App.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isLoading;
        private string _loadingMessage = "Loading...";
        private string? _errorMessage;
        private bool _hasError;
        private string? _successMessage;
        private bool _hasSuccessMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            set
            {
                SetProperty(ref _successMessage, value);
                HasSuccessMessage = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasSuccessMessage
        {
            get => _hasSuccessMessage;
            private set => SetProperty(ref _hasSuccessMessage, value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected async Task ExecuteAsync(Func<Task> action, string? loadingMessage = null)
        {
            try
            {
                IsLoading = true;
                LoadingMessage = loadingMessage ?? "Loading...";
                ErrorMessage = null;

                await action();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                ShowError(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string? loadingMessage = null)
        {
            try
            {
                IsLoading = true;
                LoadingMessage = loadingMessage ?? "Loading...";
                ErrorMessage = null;

                return await action();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                ShowError(ex.Message);
                return default;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void ShowMessage(string message, string title = "Information")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        protected void ShowError(string message, string title = "Error")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        protected void ShowWarning(string message, string title = "Warning")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        protected bool ShowConfirmation(string message, string title = "Confirm")
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Cleanup()
        {
        }
    }
}
