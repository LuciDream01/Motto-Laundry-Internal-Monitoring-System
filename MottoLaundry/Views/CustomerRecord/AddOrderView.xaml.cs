using System.Windows.Controls;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.CustomerRecord
{
    public partial class AddOrderView : UserControl
    {
        public AddOrderView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            // Set the DataContext so all your textboxes and SaveCommand work automatically
            DataContext = viewModel;

            txtCurrentDate.Text = System.DateTime.Now.ToString("MMMM dd, yyyy");
        }
    }
}