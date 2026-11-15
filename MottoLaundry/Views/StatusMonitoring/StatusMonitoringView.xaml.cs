using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using MottoLaundry.Models;
using MottoLaundry.Services;
using MottoLaundry.ViewModel;

namespace MottoLaundry.Views.StatusMonitoring
{
    public partial class StatusMonitoringView : UserControl
    {
        private readonly CustomerRecordViewModel _viewModel;
        private readonly StatusMonitoringService _service = new StatusMonitoringService();
        private OrderModel _selectedOrder;
        private bool _loading = false;

        public StatusMonitoringView(CustomerRecordViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");

            dgOrders.ItemsSource = _viewModel.Orders;
            UpdateCounts();

            _viewModel.Orders.CollectionChanged += (s, e) => UpdateCounts();
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOrder = dgOrders.SelectedItem as OrderModel;
            if (_selectedOrder == null) return;

            OrdersColumn.Width = new GridLength(1, GridUnitType.Star);
            DetailsGapColumn.Width = new GridLength(10);
            DetailsColumn.Width = new GridLength(400);
            UpdateStatusPanel.Visibility = Visibility.Visible;

            LoadSelectedOrderDetails();
        }

        private void LoadSelectedOrderDetails()
        {
            if (_selectedOrder == null) return;
            _loading = true;

            txtSelectedTracking.Text = _selectedOrder.TrackingNumber;
            txtSelectedCustomer.Text = _selectedOrder.CustomerName;
            txtSelectedService.Text = _selectedOrder.ServiceCode;
            txtServiceStatus.Text = _selectedOrder.ServiceStatus;
            txtNotes.Text = _selectedOrder.Notes;

            cmbScheduleStatus.SelectedIndex = _selectedOrder.ScheduleStatus == "Delayed" ? 1 : 0;

            // Properly map string statuses to ComboBoxItem selections
            SetComboBoxSelectedValue(cmbWashing, _selectedOrder.Washing.Status);
            SetComboBoxSelectedValue(cmbDrying, _selectedOrder.Drying.Status);
            SetComboBoxSelectedValue(cmbFolding, _selectedOrder.Folding.Status);
            SetComboBoxSelectedValue(cmbPacking, _selectedOrder.Packing.Status);

            txtWashingEmployee.Text = _selectedOrder.Washing.EmployeeNameOrId;
            txtDryingEmployee.Text = _selectedOrder.Drying.EmployeeNameOrId;
            txtFoldingEmployee.Text = _selectedOrder.Folding.EmployeeNameOrId;
            txtPackingEmployee.Text = _selectedOrder.Packing.EmployeeNameOrId;

            _loading = false;
            RefreshStageStates();
        }

        // Helper method to match string status to ComboBoxItem content
        private void SetComboBoxSelectedValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = 0; // Fallback to "Not Started"
        }

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || _selectedOrder == null) return;
            RefreshStageStates();
        }

        private void RefreshStageStates()
        {
            if (_selectedOrder == null) return;

            ComboBox[] boxes = { cmbWashing, cmbDrying, cmbFolding, cmbPacking };
            CheckBox[] checks = { chkWashing, chkDrying, chkFolding, chkPacking };

            for (int i = 0; i < 4; i++)
            {
                // Safely extract string content from ComboBoxItem for previous stage check
                string prevStatus = i > 0 ? ((ComboBoxItem)boxes[i - 1].SelectedItem)?.Content?.ToString() : "Completed";

                bool unlocked = i == 0 || (prevStatus == "Completed");
                boxes[i].IsEnabled = unlocked;

                string currentStatus = ((ComboBoxItem)boxes[i].SelectedItem)?.Content?.ToString();
                checks[i].IsChecked = (currentStatus == "Completed");
            }
        }

        private void btnUpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            string[] statuses = {
                ((ComboBoxItem)cmbWashing.SelectedItem)?.Content?.ToString() ?? "Not Started",
                ((ComboBoxItem)cmbDrying.SelectedItem)?.Content?.ToString() ?? "Not Started",
                ((ComboBoxItem)cmbFolding.SelectedItem)?.Content?.ToString() ?? "Not Started",
                ((ComboBoxItem)cmbPacking.SelectedItem)?.Content?.ToString() ?? "Not Started"
            };

            string[] employees = {
                txtWashingEmployee.Text,
                txtDryingEmployee.Text,
                txtFoldingEmployee.Text,
                txtPackingEmployee.Text
            };

            string schedule = ((ComboBoxItem)cmbScheduleStatus.SelectedItem)?.Content.ToString() ?? "On Schedule";

            try
            {
                _service.Update(_selectedOrder, statuses, employees, schedule, txtNotes.Text);
                txtServiceStatus.Text = _selectedOrder.ServiceStatus;
                RefreshGrid();
                UpdateCounts();
                MessageBox.Show("Status updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnDispatchOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _service.Dispatch(_selectedOrder);
                RefreshGrid();
                UpdateCounts();
                LoadSelectedOrderDetails();
                MessageBox.Show("Order dispatched successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot Dispatch", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Order Photo",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedOrder.PhotoPath = dialog.FileName;
                MessageBox.Show("Photo attached successfully!");
            }
        }

        private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusPanel.Visibility = Visibility.Collapsed;
            DetailsGapColumn.Width = new GridLength(0);
            DetailsColumn.Width = new GridLength(0);
            OrdersColumn.Width = new GridLength(1, GridUnitType.Star);
            dgOrders.SelectedItem = null;
            _selectedOrder = null;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgOrders?.ItemsSource == null) return;
            string search = txtSearch.Text.ToLower();
            var view = CollectionViewSource.GetDefaultView(dgOrders.ItemsSource);
            if (view == null) return;

            view.Filter = item =>
            {
                if (item is OrderModel order)
                {
                    return order.CustomerName.ToLower().Contains(search) ||
                           order.TrackingNumber.ToLower().Contains(search);
                }
                return false;
            };
        }

        private void RefreshGrid()
        {
            CollectionViewSource.GetDefaultView(dgOrders.ItemsSource)?.Refresh();
        }

        private void UpdateCounts()
        {
            foreach (var order in _viewModel.Orders)
            {
                _service.Refresh(order); // Recalculate 72hr delays and statuses
            }

            txtIntakeCount.Text = _viewModel.Orders.Count.ToString();
            txtNotStartedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Not Started").ToString();
            txtInProgressCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "In Progress").ToString();
            txtCompletedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Completed").ToString();
            txtUnclaimedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Unclaimed").ToString();
            txtDispatchedCount.Text = _viewModel.Orders.Count(x => x.ServiceStatus == "Dispatched").ToString();
        }
    }
}