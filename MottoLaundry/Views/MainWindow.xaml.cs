using System.Windows;
using System.Windows.Controls;

using MottoLaundry.Views.Dashboard;
using MottoLaundry.Views.CustomerRecord;
using MottoLaundry.Views.StatusMonitoring;
using MottoLaundry.Views.Report;

namespace MottoLaundry.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Sidebar.NavigationRequested += OnNavigationRequested;
        }

        private void OnNavigationRequested(string section)
        {
            switch (section)
            {
                case "Dashboard":
                    ChangeSection(new DashboardView());
                    break;

                case "CustomerRecord":
                    var customerRecordView = new CustomerRecordView();

                    customerRecordView.NavigateToAddOrderRequested += (s, e) =>
                    {
                        ChangeSection(new AddOrderView());
                    };

                    ChangeSection(customerRecordView);
                    break;

                case "StatusMonitoring":
                    ChangeSection(new StatusMonitoringView());
                    break;

                case "Report":
                    ChangeSection(new ReportView());
                    break;
            }
        }

        private void ChangeSection(UserControl section)
        {
            MainContent.Content = section;
        }
    }
}