
using System;
using System.Windows;
using System.Windows.Controls;

using MottoLaundry.Views.Dashboard;
using MottoLaundry.Views.CustomerRecord;
using MottoLaundry.Views.StatusMonitoring;
using MottoLaundry.Views.Report;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views
{
    public partial class MainWindow : Window
    {
        // Shared ViewModel
        private readonly CustomerRecordViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Create shared ViewModel only once
            _viewModel = new CustomerRecordViewModel();

            // Enable sidebar navigation
            Sidebar.NavigationRequested += OnNavigationRequested;

            // Display Dashboard by default
            OpenDashboard();
        }

        private void OnNavigationRequested(string section)
        {
            switch (section)
            {
                case "Dashboard":
                    OpenDashboard();
                    break;

                case "CustomerRecord":
                    OpenCustomerRecord();
                    break;

                case "StatusMonitoring":
                    ChangeSection(new StatusMonitoringView());
                    break;

                case "Report":
                    ChangeSection(new ReportView());
                    break;
            }
        }

        private void OpenDashboard()
        {
            var dashboardView = new DashboardView(_viewModel);

            ChangeSection(dashboardView);
        }

        private void OpenCustomerRecord()
        {
            var customerRecordView =
                new CustomerRecordView(_viewModel);

            customerRecordView.NavigateToAddOrderRequested +=
                (s, e) =>
                {
                    OpenAddOrder();
                };

            ChangeSection(customerRecordView);
        }

        private void OpenAddOrder()
        {
            var addOrderView = new AddOrderView(_viewModel);

            ChangeSection(addOrderView);
        }

        private void ChangeSection(UserControl section)
        {
            MainContent.Content = section;
        }
    }
}
