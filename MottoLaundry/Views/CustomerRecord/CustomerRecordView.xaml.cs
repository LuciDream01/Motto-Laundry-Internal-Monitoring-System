
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using CustomerRecordModel = MottoLaundry.Models.Customer;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class CustomerRecordView : UserControl
    {
        // Collection of customer records
        private ObservableCollection<CustomerRecordModel> records =
            new ObservableCollection<CustomerRecordModel>();

        public CustomerRecordView()
        {
            InitializeComponent();

            //// Current Date Calendar
            //txtCurrentDate.Text =
            //    DateTime.Now.ToString("MMMM dd, yyyy");

            //// Connect collection to DataGrid
            //dgCustomerRecords.ItemsSource = records;

            //// Sample data
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
    }
}
