
using MottoLaundry.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class CustomerRecordView : UserControl
    {
        // Collection of customer 

        //  Event handler for switching to Add new order Interface
        public event EventHandler NavigateToAddOrderRequested;

        public CustomerRecordView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            // Current Date Calendar
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");

            // Retrieving Data from View Model to Display on the DataGridView
            DataContext = viewModel;
            dgCustomerRecords.ItemsSource = viewModel.CustomerRecordData;
            
        }
        public void btnAddNewOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigateToAddOrderRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
