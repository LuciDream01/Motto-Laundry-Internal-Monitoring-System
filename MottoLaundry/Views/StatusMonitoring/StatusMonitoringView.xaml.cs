
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using MottoLaundry.Models;
using MottoLaundry.Services;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.StatusMonitoring
{
    public partial class StatusMonitoringView : UserControl
    {
        private readonly CustomerRecordViewModel _customerViewModel;
        private readonly StatusMonitoringViewModel _statusViewModel;
        private readonly StatusMonitoringService _statusService;
        private readonly ICollectionView _ordersView;

        private readonly HashSet<OrderModel> _subscribedOrders =
            new HashSet<OrderModel>();

        private OrderModel _selectedOrder;

        private string _selectedStatus = "All";

        private bool _loading;
        private bool _closingDetails;
        private bool _refreshingGrid;
        private bool _refreshPending;
        private bool _suppressOrderNotifications;
        private bool _operationInProgress;

        // =====================================================
        // SECTION 1: INITIALIZATION
        // =====================================================

        public StatusMonitoringView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();

            _customerViewModel = viewModel ??
                throw new ArgumentNullException(nameof(viewModel));

            _statusViewModel = new StatusMonitoringViewModel(
                _customerViewModel.Orders);

            _statusService = new StatusMonitoringService();

            txtCurrentDate.Text =
                DateTime.Now.ToString("MMMM dd, yyyy");

            // Recalculate initial values once.
            // Suppress notifications while preparing the view.
            _suppressOrderNotifications = true;

            try
            {
                foreach (OrderModel order in _statusViewModel.Orders)
                {
                    _statusViewModel.RefreshOrder(order);
                }
            }
            finally
            {
                _suppressOrderNotifications = false;
            }

            // Separate view for filtering.
            CollectionViewSource source = new CollectionViewSource
            {
                Source = _statusViewModel.Orders
            };

            _ordersView = source.View;
            _ordersView.Filter = FilterOrder;

            dgOrders.ItemsSource = _ordersView;

            _statusViewModel.Orders.CollectionChanged +=
                Orders_CollectionChanged;

            foreach (OrderModel order in _statusViewModel.Orders)
            {
                SubscribeOrder(order);
            }

            Unloaded += StatusMonitoringView_Unloaded;

            UpdateCounts();
            UpdateSelectedCardAppearance();
        }

        // =====================================================
        // SECTION 2: FILTER ORDERS
        // =====================================================

        private bool FilterOrder(object item)
        {
            if (item is not OrderModel order)
                return false;

            bool matchesStatus =
                _selectedStatus == "All" ||
                string.Equals(
                    order.ServiceStatus,
                    _selectedStatus,
                    StringComparison.OrdinalIgnoreCase);

            string search = txtSearch?.Text?.Trim() ?? "";

            bool matchesSearch =
                string.IsNullOrEmpty(search) ||
                (order.CustomerName ?? "").Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                (order.TrackingNumber ?? "").Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase);

            return matchesStatus && matchesSearch;
        }

        // =====================================================
        // SECTION 3: STATUS CARD CLICK
        // =====================================================

        private void StatusCard_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border card)
                return;

            string status = card.Tag?.ToString() ?? "All";

            _selectedStatus =
                _selectedStatus == status && status != "All"
                    ? "All"
                    : status;

            UpdateSelectedCardAppearance();

            // Filtering is a user action, so refresh directly.
            RefreshGrid();
        }

        // =====================================================
        // SECTION 4: CARD HIGHLIGHT
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
                bool selected =
                    (card.Tag?.ToString() ?? "All") ==
                    _selectedStatus;

                card.BorderBrush = selected
                    ? new SolidColorBrush(
                        Color.FromRgb(8, 6, 94))
                    : Brushes.Transparent;

                card.BorderThickness = selected
                    ? new Thickness(2)
                    : new Thickness(0);
            }
        }

        // =====================================================
        // SECTION 5: PROPERTY SUBSCRIPTIONS
        // =====================================================

        private void SubscribeOrder(OrderModel order)
        {
            if (order == null)
                return;

            if (_subscribedOrders.Add(order))
            {
                order.PropertyChanged += Order_PropertyChanged;
            }
        }

        private void UnsubscribeOrder(OrderModel order)
        {
            if (order == null)
                return;

            if (_subscribedOrders.Remove(order))
            {
                order.PropertyChanged -= Order_PropertyChanged;
            }
        }

        // =====================================================
        // SECTION 6: COLLECTION CHANGES
        // =====================================================

        private void Orders_CollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (OrderModel order in e.OldItems)
                {
                    UnsubscribeOrder(order);
                }
            }

            if (e.NewItems != null)
            {
                foreach (OrderModel order in e.NewItems)
                {
                    SubscribeOrder(order);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (OrderModel oldOrder in
                    _subscribedOrders.ToArray())
                {
                    UnsubscribeOrder(oldOrder);
                }

                foreach (OrderModel order in _statusViewModel.Orders)
                {
                    SubscribeOrder(order);
                }
            }

            UpdateCounts();
            ScheduleGridRefresh();
        }

        // =====================================================
        // SECTION 7: PROPERTY CHANGES
        // =====================================================

        private void Order_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (_suppressOrderNotifications)
                return;

            string property = e.PropertyName;

            if (string.IsNullOrEmpty(property) ||
                property == nameof(OrderModel.ServiceStatus))
            {
                UpdateCounts();
                ScheduleGridRefresh();
            }
            else if (
                property == nameof(OrderModel.CustomerName) ||
                property == nameof(OrderModel.TrackingNumber))
            {
                ScheduleGridRefresh();
            }

            // No refresh needed for every ProgressStage,
            // ScheduleStatus or Notes change.
            // WPF bindings update their visible cells.
        }

        // =====================================================
        // SECTION 8: COALESCED GRID REFRESH
        // =====================================================

        private void ScheduleGridRefresh()
        {
            if (_refreshPending)
                return;

            _refreshPending = true;

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _refreshPending = false;

                    if (!IsLoaded)
                        return;

                    RefreshGrid();
                }),
                DispatcherPriority.Background);
        }

        private void RefreshGrid()
        {
            if (_ordersView == null || _refreshingGrid)
                return;

            _refreshingGrid = true;

            try
            {
                _ordersView.Refresh();
            }
            finally
            {
                _refreshingGrid = false;
            }

            // A saved status change can move the selected
            // order outside the currently active filter.
            if (_selectedOrder != null &&
                !FilterOrder(_selectedOrder))
            {
                CloseDetailsPanel();
            }
        }

        // =====================================================
        // SECTION 9: SELECT ORDER
        // =====================================================

        private void dgOrders_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_closingDetails || _refreshingGrid)
                return;

            OrderModel selected =
                dgOrders.SelectedItem as OrderModel;

            if (selected == null)
            {
                if (_selectedOrder != null &&
                    !_operationInProgress)
                {
                    CloseDetailsPanel();
                }

                return;
            }

            _selectedOrder = selected;
            _statusViewModel.SelectedOrder = selected;

            OrdersColumn.Width =
                new GridLength(1, GridUnitType.Star);

            DetailsGapColumn.Width =
                new GridLength(10);

            DetailsColumn.Width =
                new GridLength(400);

            UpdateStatusPanel.Visibility =
                Visibility.Visible;

            LoadSelectedOrderDetails();
        }

        // =====================================================
        // SECTION 10: LOAD SELECTED ORDER
        // =====================================================

        private void LoadSelectedOrderDetails()
        {
            if (_selectedOrder == null)
                return;

            _loading = true;
            _suppressOrderNotifications = true;

            try
            {
                // Refresh only the selected order.
                _statusViewModel.RefreshOrder(_selectedOrder);

                txtSelectedTracking.Text =
                    _selectedOrder.TrackingNumber;

                txtSelectedCustomer.Text =
                    _selectedOrder.CustomerName;

                txtSelectedService.Text =
                    _selectedOrder.ServiceCode;

                txtServiceStatus.Text =
                    _selectedOrder.ServiceStatus;

                txtScheduleStatus.Text =
                    _selectedOrder.ScheduleStatus;

                txtNotes.Text =
                    _selectedOrder.Notes;

                SetComboBoxSelectedValue(
                    cmbWashing,
                    _selectedOrder.Washing.Status);

                SetComboBoxSelectedValue(
                    cmbDrying,
                    _selectedOrder.Drying.Status);

                SetComboBoxSelectedValue(
                    cmbFolding,
                    _selectedOrder.Folding.Status);

                SetComboBoxSelectedValue(
                    cmbPacking,
                    _selectedOrder.Packing.Status);

                txtWashingEmployee.Text =
                    _selectedOrder.Washing.EmployeeNameOrId;

                txtDryingEmployee.Text =
                    _selectedOrder.Drying.EmployeeNameOrId;

                txtFoldingEmployee.Text =
                    _selectedOrder.Folding.EmployeeNameOrId;

                txtPackingEmployee.Text =
                    _selectedOrder.Packing.EmployeeNameOrId;

                bool editable = !_selectedOrder.IsDispatched;

                txtWashingEmployee.IsReadOnly = !editable;
                txtDryingEmployee.IsReadOnly = !editable;
                txtFoldingEmployee.IsReadOnly = !editable;
                txtPackingEmployee.IsReadOnly = !editable;
                txtNotes.IsReadOnly = !editable;

                btnUpdateStatus.IsEnabled = editable;
                btnDispatchOrder.IsEnabled = editable;
                btnUploadPhoto.IsEnabled = editable;
            }
            finally
            {
                _suppressOrderNotifications = false;
                _loading = false;
            }

            RefreshStageStates();
        }

        // =====================================================
        // SECTION 11: COMBOBOX HELPERS
        // =====================================================

        private static void SetComboBoxSelectedValue(
            ComboBox comboBox,
            string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private static string GetComboBoxValue(
            ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "Not Started";
        }

        private string[] GetSelectedStatuses()
        {
            return new[]
            {
                GetComboBoxValue(cmbWashing),
                GetComboBoxValue(cmbDrying),
                GetComboBoxValue(cmbFolding),
                GetComboBoxValue(cmbPacking)
            };
        }

        // =====================================================
        // SECTION 12: STAGE CHANGE
        // =====================================================

        private void StageCombo_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading || _selectedOrder == null)
                return;

            RefreshStageStates();
            UpdateServiceStatusPreview();
        }

        private void UpdateServiceStatusPreview()
        {
            if (_selectedOrder == null)
                return;

            if (_selectedOrder.IsDispatched)
            {
                txtServiceStatus.Text = "Dispatched";
                return;
            }

            txtServiceStatus.Text =
                _statusService.CalculateServiceStatus(
                    GetSelectedStatuses());
        }

        // =====================================================
        // SECTION 13: STAGE STATES
        // =====================================================

        private void RefreshStageStates()
        {
            if (_selectedOrder == null)
                return;

            ComboBox[] boxes =
            {
                cmbWashing,
                cmbDrying,
                cmbFolding,
                cmbPacking
            };

            CheckBox[] checks =
            {
                chkWashing,
                chkDrying,
                chkFolding,
                chkPacking
            };

            for (int i = 0; i < 4; i++)
            {
                string previousStatus =
                    i == 0
                        ? "Completed"
                        : GetComboBoxValue(boxes[i - 1]);

                bool unlocked =
                    !_selectedOrder.IsDispatched &&
                    (i == 0 || previousStatus == "Completed");

                boxes[i].IsEnabled = unlocked;

                checks[i].IsChecked =
                    GetComboBoxValue(boxes[i]) == "Completed";
            }
        }

        // =====================================================
        // SECTION 14: UPDATE STATUS
        // =====================================================

        private async void btnUpdateStatus_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedOrder == null || _operationInProgress)
                return;

            OrderModel orderBeingUpdated = _selectedOrder;

            string[] statuses = GetSelectedStatuses();

            string[] employees =
            {
                txtWashingEmployee.Text,
                txtDryingEmployee.Text,
                txtFoldingEmployee.Text,
                txtPackingEmployee.Text
            };

            _operationInProgress = true;
            btnUpdateStatus.IsEnabled = false;

            try
            {
                await _statusViewModel.UpdateAsync(
                    statuses,
                    employees,
                    txtNotes.Text);

                // Status may have changed while saving.
                if (_selectedOrder == orderBeingUpdated)
                {
                    LoadSelectedOrderDetails();
                }

                UpdateCounts();

                MessageBox.Show(
                    "Order status saved to database successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Update Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _operationInProgress = false;

                btnUpdateStatus.IsEnabled =
                    _selectedOrder != null &&
                    !_selectedOrder.IsDispatched;

                ScheduleGridRefresh();
            }
        }

        // =====================================================
        // SECTION 15: DISPATCH
        // =====================================================

        private async void btnDispatchOrder_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedOrder == null || _operationInProgress)
                return;

            OrderModel orderBeingDispatched = _selectedOrder;

            _operationInProgress = true;
            btnDispatchOrder.IsEnabled = false;

            try
            {
                await _statusViewModel.DispatchAsync();

                if (_selectedOrder == orderBeingDispatched)
                {
                    LoadSelectedOrderDetails();
                }

                UpdateCounts();

                MessageBox.Show(
                    "Order dispatched successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Cannot Dispatch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _operationInProgress = false;

                btnDispatchOrder.IsEnabled =
                    _selectedOrder != null &&
                    !_selectedOrder.IsDispatched;

                ScheduleGridRefresh();
            }
        }

        // =====================================================
        // SECTION 16: UPLOAD PHOTO
        // =====================================================

        private async void btnUploadPhoto_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedOrder == null || _operationInProgress)
                return;

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Order Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() != true)
                return;

            _operationInProgress = true;
            btnUploadPhoto.IsEnabled = false;

            try
            {
                await _statusViewModel.UploadPhotoAsync(
                    dialog.FileName);

                MessageBox.Show(
                    "Photo saved successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Upload Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _operationInProgress = false;

                btnUploadPhoto.IsEnabled =
                    _selectedOrder != null &&
                    !_selectedOrder.IsDispatched;
            }
        }

        // =====================================================
        // SECTION 17: CLOSE DETAILS
        // =====================================================

        private void btnCloseDetails_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_operationInProgress)
                return;

            CloseDetailsPanel();
        }

        private void CloseDetailsPanel()
        {
            if (_closingDetails)
                return;

            _closingDetails = true;

            try
            {
                UpdateStatusPanel.Visibility =
                    Visibility.Collapsed;

                DetailsGapColumn.Width = new GridLength(0);
                DetailsColumn.Width = new GridLength(0);

                OrdersColumn.Width =
                    new GridLength(1, GridUnitType.Star);

                _selectedOrder = null;
                _statusViewModel.SelectedOrder = null;

                dgOrders.SelectedItem = null;
            }
            finally
            {
                _closingDetails = false;
            }
        }

        // =====================================================
        // SECTION 18: SEARCH
        // =====================================================

        private void txtSearch_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ScheduleGridRefresh();
        }

        // =====================================================
        // SECTION 19: UPDATE COUNTS
        // =====================================================

        private void UpdateCounts()
        {
            if (_statusViewModel == null)
                return;

            // Only count.
            // Never recalculate every order here.

            var orders = _statusViewModel.Orders;

            int intake = 0;
            int notStarted = 0;
            int inProgress = 0;
            int completed = 0;
            int unclaimed = 0;
            int dispatched = 0;

            foreach (OrderModel order in orders)
            {
                intake++;

                switch (order.ServiceStatus)
                {
                    case "Not Started":
                        notStarted++;
                        break;

                    case "In Progress":
                        inProgress++;
                        break;

                    case "Completed":
                        completed++;
                        break;

                    case "Unclaimed":
                        unclaimed++;
                        break;

                    case "Dispatched":
                        dispatched++;
                        break;
                }
            }

            txtIntakeCount.Text = intake.ToString();
            txtNotStartedCount.Text = notStarted.ToString();
            txtInProgressCount.Text = inProgress.ToString();
            txtCompletedCount.Text = completed.ToString();
            txtUnclaimedCount.Text = unclaimed.ToString();
            txtDispatchedCount.Text = dispatched.ToString();
        }

        // =====================================================
        // SECTION 20: CLEANUP
        // =====================================================

        private void StatusMonitoringView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            _statusViewModel.Orders.CollectionChanged -=
                Orders_CollectionChanged;

            foreach (OrderModel order in _subscribedOrders.ToArray())
            {
                UnsubscribeOrder(order);
            }

            Unloaded -= StatusMonitoringView_Unloaded;
        }
    }
}
