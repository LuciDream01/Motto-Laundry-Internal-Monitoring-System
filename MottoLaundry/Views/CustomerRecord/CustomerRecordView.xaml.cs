
using System;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CustomerRecordModel = MottoLaundry.Models.Orders;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class CustomerRecordView : UserControl
    {
        // Collection of customer records
        private ObservableCollection<CustomerRecordModel> records =
            new ObservableCollection<CustomerRecordModel>();

        //  Event handler for switching to Add new order Interface
        public event EventHandler NavigateToAddOrderRequested;

        public CustomerRecordView()
        {
            InitializeComponent();

            // Current Date Calendar
            txtCurrentDate.Text =
                DateTime.Now.ToString("MMMM dd, yyyy");

            // Connect collection to DataGrid
            dgCustomerRecords.ItemsSource = records;

            // Sample data
            //records.Add(new CustomerRecordModel
            //{
            //    TrackingNumber = "TRK-001",
            //    CustomerName = "Juan Dela Cruz",
            //    ServiceCode = "FULL",
            //    Load = 5.5
            //});

            //records.Add(new CustomerRecordModel
            //{
            //    TrackingNumber = "TRK-002",
            //    CustomerName = "Maria Santos",
            //    ServiceCode = "SELF",
            //    Load = 3.0
            //});
        }
        public void btnAddNewOrder_Click(object sender, RoutedEventArgs e)
        {
            NavigateToAddOrderRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
