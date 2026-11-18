
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MottoLaundry.Models;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        // =====================================================
        // FIELDS
        // =====================================================

        private readonly CustomerRecordViewModel _viewModel;
        private readonly ICollectionView _ordersView;

        private string _selectedStatus = "All";

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public DashboardView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ??
                throw new ArgumentNullException(nameof(viewModel));

            txtCurrentDate.Text =
                DateTime.Now.ToString("MMMM dd, yyyy");

            // Independent view for Dashboard filtering.
            // The original Orders collection is not modified.
            CollectionViewSource source = new CollectionViewSource
            {
                Source = _viewModel.Orders
            };

            _ordersView = source.View;
            _ordersView.Filter = FilterOrder;

            dgOrders.ItemsSource = _ordersView;

            // Update counts when orders are added or removed.
            _viewModel.Orders.CollectionChanged +=
                Orders_CollectionChanged;

            // Update counts and filtering when an existing
            // order changes its ServiceStatus.
            foreach (OrderModel order in _viewModel.Orders)
            {
                order.PropertyChanged += Order_PropertyChanged;
            }

            UpdateDashboardCounts();
            UpdateSelectedCardAppearance();
        }

        // =====================================================
        // FILTER ORDERS
        // =====================================================

        private bool FilterOrder(object item)
        {
            if (item is not OrderModel order)
                return false;

            // STATUS FILTER
            bool matchesStatus =
                _selectedStatus == "All" ||
                string.Equals(
                    order.ServiceStatus,
                    _selectedStatus,
                    StringComparison.OrdinalIgnoreCase);

            // SEARCH FILTER
            string search =
                txtSearch?.Text?.Trim() ?? "";

            bool matchesSearch =
                string.IsNullOrWhiteSpace(search) ||

                (order.TrackingNumber ?? "").Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||

                (order.CustomerName ?? "").Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase);

            return matchesStatus && matchesSearch;
        }

        // =====================================================
        // STATUS CARD CLICK
        // =====================================================

        private void StatusCard_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border selectedCard)
                return;

            string status =
                selectedCard.Tag?.ToString() ?? "All";

            // Clicking the same card again resets to All.
            if (_selectedStatus == status &&
                status != "All")
            {
                _selectedStatus = "All";
            }
            else
            {
                _selectedStatus = status;
            }

            // Apply filtering to the DataGrid.
            _ordersView.Refresh();

            // Highlight the selected status card.
            UpdateSelectedCardAppearance();
        }

        // =====================================================
        // SEARCH TEXT CHANGED
        // =====================================================

        private void txtSearch_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            _ordersView?.Refresh();
        }

        // =====================================================
        // SELECTED CARD APPEARANCE
        // =====================================================

        private void UpdateSelectedCardAppearance()
        {
            Border[] cards =
            {
                cardIntake,
                cardNotStarted,
                cardInProgress,
                cardCompleted,
                cardUnclaimed,
                cardDispatched
            };

            foreach (Border card in cards)
            {
                string status =
                    card.Tag?.ToString() ?? "All";

                bool isSelected =
                    status == _selectedStatus;

                card.BorderBrush = isSelected
                    ? new SolidColorBrush(
                        Color.FromRgb(8, 6, 94))
                    : Brushes.Transparent;

                card.BorderThickness = isSelected
                    ? new Thickness(2)
                    : new Thickness(0);
            }
        }

        // =====================================================
        // ORDERS COLLECTION CHANGED
        // =====================================================

        private void Orders_CollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            // Unsubscribe removed orders.
            if (e.OldItems != null)
            {
                foreach (OrderModel order in e.OldItems)
                {
                    order.PropertyChanged -=
                        Order_PropertyChanged;
                }
            }

            // Subscribe newly added orders.
            if (e.NewItems != null)
            {
                foreach (OrderModel order in e.NewItems)
                {
                    order.PropertyChanged -=
                        Order_PropertyChanged;

                    order.PropertyChanged +=
                        Order_PropertyChanged;
                }
            }

            // Handle a complete collection reset.
            if (e.Action ==
                NotifyCollectionChangedAction.Reset)
            {
                foreach (OrderModel order in _viewModel.Orders)
                {
                    order.PropertyChanged -=
                        Order_PropertyChanged;

                    order.PropertyChanged +=
                        Order_PropertyChanged;
                }
            }

            UpdateDashboardCounts();
            _ordersView.Refresh();
        }

        // =====================================================
        // ORDER PROPERTY CHANGED
        // =====================================================

        private void Order_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(OrderModel.ServiceStatus))
            {
                UpdateDashboardCounts();
                _ordersView.Refresh();
            }
            else if (
                e.PropertyName == nameof(OrderModel.CustomerName) ||
                e.PropertyName == nameof(OrderModel.TrackingNumber))
            {
                _ordersView.Refresh();
            }
        }

        // =====================================================
        // UPDATE DASHBOARD COUNTS
        // =====================================================

        private void UpdateDashboardCounts()
        {
            var orders = _viewModel.Orders;

            txtIntakeCount.Text =
                orders.Count.ToString();

            txtNotStartedCount.Text =
                orders.Count(x =>
                    x.ServiceStatus == "Not Started")
                .ToString();

            txtInProgressCount.Text =
                orders.Count(x =>
                    x.ServiceStatus == "In Progress")
                .ToString();

            txtCompletedCount.Text =
                orders.Count(x =>
                    x.ServiceStatus == "Completed")
                .ToString();

            txtUnclaimedCount.Text =
                orders.Count(x =>
                    x.ServiceStatus == "Unclaimed")
                .ToString();

            txtDispatchedCount.Text =
                orders.Count(x =>
                    x.ServiceStatus == "Dispatched")
                .ToString();
        }
    }
}
