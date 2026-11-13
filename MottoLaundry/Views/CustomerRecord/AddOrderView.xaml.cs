
using System;
using System.Windows.Controls;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class AddOrderView : UserControl
    {
        public AddOrderView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            // Display current date
            txtCurrentDate.Text =
                DateTime.Now.ToString("MMMM dd, yyyy");

            // Use shared ViewModel from MainWindow
            DataContext = viewModel;
        }
    }
}
