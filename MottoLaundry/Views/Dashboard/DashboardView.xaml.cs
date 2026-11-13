using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MottoLaundry.ViewModel;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>

    public partial class DashboardView : UserControl
    {
        public DashboardView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            // Retrieving Data from View Model to Display on the DataGridView
            DataContext = viewModel;
            dgOrders.ItemsSource = viewModel.DashboardData;
            
        }
    }
}
