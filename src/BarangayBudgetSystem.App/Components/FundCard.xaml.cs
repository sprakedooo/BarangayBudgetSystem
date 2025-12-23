using System.Windows;
using System.Windows.Controls;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Components
{
    public partial class FundCard : UserControl
    {
        public static readonly DependencyProperty FundProperty =
            DependencyProperty.Register(
                nameof(Fund),
                typeof(AppropriationFund),
                typeof(FundCard),
                new PropertyMetadata(null));

        public FundCard()
        {
            InitializeComponent();
        }

        public AppropriationFund? Fund
        {
            get => (AppropriationFund?)GetValue(FundProperty);
            set => SetValue(FundProperty, value);
        }
    }
}
