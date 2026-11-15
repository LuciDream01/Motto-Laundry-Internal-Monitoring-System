using System;
using System.Windows;
using System.Windows.Controls;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class CustomerRecordView : UserControl
    {
        private readonly CustomerRecordViewModel _viewModel;

        // Event that MainWindow is listening for
        public event EventHandler? NavigateToAddOrderRequested;

        public CustomerRecordView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;

            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");

            // Bind the DataGrid to the shared Orders collection in the ViewModel
            dgCustomerRecords.ItemsSource = _viewModel.Orders;
        }

        private void btnAddNewOrder_Click(object sender, RoutedEventArgs e)
        {
            // Trigger the event so MainWindow switches to AddOrderView
            NavigateToAddOrderRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}