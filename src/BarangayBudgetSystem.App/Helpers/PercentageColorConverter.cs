using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BarangayBudgetSystem.App.Helpers
{
    public class PercentageColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return percentage switch
                {
                    >= 90 => new SolidColorBrush(Color.FromRgb(220, 53, 69)),    // Red - Critical
                    >= 75 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Yellow - Warning
                    >= 50 => new SolidColorBrush(Color.FromRgb(23, 162, 184)),   // Cyan - Moderate
                    _ => new SolidColorBrush(Color.FromRgb(40, 167, 69))          // Green - Good
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageToProgressColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return percentage switch
                {
                    >= 90 => new SolidColorBrush(Color.FromRgb(220, 53, 69)),    // Red
                    >= 75 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Yellow
                    _ => new SolidColorBrush(Color.FromRgb(40, 167, 69))          // Green
                };
            }

            return new SolidColorBrush(Color.FromRgb(0, 123, 255)); // Blue default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "pending" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),       // Gray
                    "for approval" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Yellow
                    "approved" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),        // Green
                    "rejected" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),        // Red
                    "cancelled" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),     // Gray
                    "completed" => new SolidColorBrush(Color.FromRgb(23, 162, 184)),      // Cyan
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))                 // Gray default
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                return (boolValue != invert) ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                return (visibility == Visibility.Visible) != invert;
            }

            return false;
        }
    }

    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                return amount.ToString("N2", new CultureInfo("en-PH"));
            }

            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && decimal.TryParse(text, NumberStyles.Currency, new CultureInfo("en-PH"), out decimal result))
            {
                return result;
            }

            return 0m;
        }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return $"{percentage:F1}%";
            }

            return "0.0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString()?.ToLower() == "invert";
            bool isNull = value == null;

            return (isNull == invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return false;
        }
    }

    public class MultiValueEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            return values[0]?.Equals(values[1]) ?? values[1] == null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CategoryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                return category switch
                {
                    "General Fund" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),        // #3498db
                    "Special Education Fund" => new SolidColorBrush(Color.FromRgb(155, 89, 182)),  // #9b59b6
                    "Trust Fund" => new SolidColorBrush(Color.FromRgb(26, 188, 156)),          // #1abc9c
                    "SK Fund" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),              // #e74c3c
                    "Disaster Fund" => new SolidColorBrush(Color.FromRgb(230, 126, 34)),       // #e67e22
                    "Development Fund" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),    // #2ecc71
                    "Personnel Services" => new SolidColorBrush(Color.FromRgb(52, 73, 94)),    // #34495e
                    "MOOE" => new SolidColorBrush(Color.FromRgb(243, 156, 18)),                // #f39c12
                    "Capital Outlay" => new SolidColorBrush(Color.FromRgb(22, 160, 133)),      // #16a085
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))                      // Gray default
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CategoryToShortNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                return category switch
                {
                    "General Fund" => "GF",
                    "Special Education Fund" => "SEF",
                    "Trust Fund" => "TF",
                    "SK Fund" => "SK",
                    "Disaster Fund" => "DF",
                    "Development Fund" => "DEV",
                    "Personnel Services" => "PS",
                    "MOOE" => "MOOE",
                    "Capital Outlay" => "CO",
                    _ => "OTH"
                };
            }

            return "OTH";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is double percentage &&
                values[1] is double containerWidth)
            {
                // Cap at 100% and calculate width
                var clampedPercentage = Math.Min(Math.Max(percentage, 0), 100);
                return containerWidth * (clampedPercentage / 100);
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to show/hide action buttons based on transaction status.
    /// Parameter values: Edit, Submit, Approve, Reject, Delete
    /// </summary>
    public class StatusToButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string status || parameter is not string action)
                return Visibility.Collapsed;

            return action.ToLower() switch
            {
                // Edit and Submit are available only for Pending transactions
                "edit" => status == "Pending" ? Visibility.Visible : Visibility.Collapsed,
                "submit" => status == "Pending" ? Visibility.Visible : Visibility.Collapsed,
                "delete" => status == "Pending" ? Visibility.Visible : Visibility.Collapsed,

                // Approve and Reject are available only for "For Approval" transactions
                "approve" => status == "For Approval" ? Visibility.Visible : Visibility.Collapsed,
                "reject" => status == "For Approval" ? Visibility.Visible : Visibility.Collapsed,

                _ => Visibility.Collapsed
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
