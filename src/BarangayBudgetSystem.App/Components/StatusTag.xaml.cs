using System.Windows;
using System.Windows.Controls;

namespace BarangayBudgetSystem.App.Components
{
    public partial class StatusTag : UserControl
    {
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(StatusTag),
                new PropertyMetadata(string.Empty));

        public StatusTag()
        {
            InitializeComponent();
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }
    }
}
