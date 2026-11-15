using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using MottoLaundry.Models;
using MottoLaundry.ViewModel; // Make sure this namespace is included

namespace MottoLaundry.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        private readonly CustomerRecordViewModel _viewModel;

        public DashboardView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");

            dgOrders.ItemsSource = _viewModel.Orders;
            UpdateDashboardCounts();

            _viewModel.Orders.CollectionChanged += (s, e) => UpdateDashboardCounts();
        }

        private void UpdateDashboardCounts()
        {
            txtIntakeCount.Text = _viewModel.Orders.Count.ToString();
            txtNotStartedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Not Started").ToString();
            txtInProgressCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "In Progress").ToString();
            txtCompletedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Completed").ToString();
            txtUnclaimedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Unclaimed").ToString();
            txtDispatchedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Dispatched").ToString();
        }
    }
}