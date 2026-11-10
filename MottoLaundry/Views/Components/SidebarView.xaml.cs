
using System;
using System.Windows;
using System.Windows.Controls;

namespace MottoLaundry.Views.Components
{
    /// <summary>
    /// Interaction logic for SidebarView.xaml
    /// </summary>
    public partial class SidebarView : UserControl
    {
        // Event that notifies MainWindow
        // whenever a navigation button is clicked.
        public event Action<string>? NavigationRequested;

        public SidebarView()
        {
            InitializeComponent();
        }

        // DASHBOARD
        private void BtnDashboard_Click(
            object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("Dashboard");
        }

        // CUSTOMER RECORD
        private void BtnCustomerRecord_Click(
            object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("CustomerRecord");
        }

        // ORDER MONITORING
        private void BtnStatusMonitoring_Click(
            object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("StatusMonitoring");
        }

        // REPORT
        private void BtnReport_Click(
            object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke("Report");
        }
    }
}
