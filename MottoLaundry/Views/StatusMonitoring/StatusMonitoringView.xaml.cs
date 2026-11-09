
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace MottoLaundry.Views.StatusMonitoring
{
    public partial class StatusMonitoringView : UserControl
    {
        private readonly ObservableCollection<StatusOrder> orders =
            new ObservableCollection<StatusOrder>();

        private ICollectionView ordersView;
        private StatusOrder selectedOrder;

        public StatusMonitoringView()
        {
            InitializeComponent();

            txtCurrentDate.Text =
                DateTime.Now.ToString("MMMM dd, yyyy");

            InitializeStageComboBoxes();
            LoadSampleOrders();

            ordersView = CollectionViewSource.GetDefaultView(orders);
            ordersView.Filter = FilterOrders;

            dgOrders.ItemsSource = ordersView;
        }

        private void InitializeStageComboBoxes()
        {
            ComboBox[] comboBoxes =
            {
                cmbWashing,
                cmbDrying,
                cmbFolding,
                cmbPacking
            };

            foreach (ComboBox combo in comboBoxes)
            {
                combo.Items.Add("Not Started");
                combo.Items.Add("Current");
                combo.Items.Add("Completed");

                combo.SelectedIndex = 0;
            }
        }

        private void LoadSampleOrders()
        {
            orders.Add(new StatusOrder
            {
                TrackingNumber = "TRK-001",
                CustomerName = "Juan Dela Cruz",
                ServiceCode = "FS",
                ServiceName = "Full Service",
                ServiceStatus = "In Progress",
                ProgressStage = "Folding",
                TurnaroundTime = "48 Hours",
                ScheduleStatus = "On Schedule",
                Washing = "Completed",
                Drying = "Completed",
                Folding = "Current",
                Packing = "Not Started"
            });

            orders.Add(new StatusOrder
            {
                TrackingNumber = "TRK-002",
                CustomerName = "Maria Santos",
                ServiceCode = "SS",
                ServiceName = "Self Service",
                ServiceStatus = "Not Started",
                ProgressStage = "Pending",
                TurnaroundTime = "24 Hours",
                ScheduleStatus = "On Schedule"
            });

            orders.Add(new StatusOrder
            {
                TrackingNumber = "TRK-003",
                CustomerName = "Pedro Reyes",
                ServiceCode = "FS",
                ServiceName = "Full Service",
                ServiceStatus = "Completed",
                ProgressStage = "Packing",
                TurnaroundTime = "48 Hours",
                ScheduleStatus = "Delayed",
                Washing = "Completed",
                Drying = "Completed",
                Folding = "Completed",
                Packing = "Completed"
            });
        }

        // SEARCH FUNCTION
        private bool FilterOrders(object item)
        {
            if (!(item is StatusOrder order))
                return false;

            string search = txtSearch.Text.Trim();

            if (string.IsNullOrWhiteSpace(search))
                return true;

            return order.TrackingNumber.Contains(
                       search, StringComparison.OrdinalIgnoreCase)
                   || order.CustomerName.Contains(
                       search, StringComparison.OrdinalIgnoreCase)
                   || order.ServiceStatus.Contains(
                       search, StringComparison.OrdinalIgnoreCase);
        }

        private void txtSearch_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ordersView?.Refresh();
        }

        // SELECT ORDER FROM DATAGRID
        private void dgOrders_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!(dgOrders.SelectedItem is StatusOrder order))
                return;

            selectedOrder = order;

            ShowUpdatePanel();
            LoadSelectedOrder(order);
        }

        // SHRINK DATAGRID AND SHOW UPDATE PANEL
        private void ShowUpdatePanel()
        {
            OrdersColumn.Width =
                new GridLength(3, GridUnitType.Star);

            DetailsGapColumn.Width =
                new GridLength(15);

            DetailsColumn.Width =
                new GridLength(2, GridUnitType.Star);

            UpdateStatusPanel.Visibility = Visibility.Visible;
        }

        // EXPAND DATAGRID BACK TO FULL WIDTH
        private void HideUpdatePanel()
        {
            UpdateStatusPanel.Visibility = Visibility.Collapsed;

            DetailsGapColumn.Width = new GridLength(0);
            DetailsColumn.Width = new GridLength(0);

            OrdersColumn.Width =
                new GridLength(1, GridUnitType.Star);

            selectedOrder = null;
            dgOrders.SelectedItem = null;
        }

        private void btnCloseDetails_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideUpdatePanel();
        }

        // DISPLAY SELECTED ORDER DETAILS
        private void LoadSelectedOrder(StatusOrder order)
        {
            txtSelectedTracking.Text = order.TrackingNumber;
            txtSelectedCustomer.Text = order.CustomerName;
            txtSelectedService.Text = order.ServiceName;

            SelectComboItem(cmbServiceStatus, order.ServiceStatus);
            SelectComboItem(cmbScheduleStatus, order.ScheduleStatus);

            cmbWashing.SelectedItem = order.Washing;
            cmbDrying.SelectedItem = order.Drying;
            cmbFolding.SelectedItem = order.Folding;
            cmbPacking.SelectedItem = order.Packing;

            txtNotes.Text = order.Notes;
        }

        private void SelectComboItem(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private string GetComboItem(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "";
        }

        // UPDATE SELECTED ORDER
        private void btnUpdateStatus_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (selectedOrder == null)
                return;

            selectedOrder.ServiceStatus =
                GetComboItem(cmbServiceStatus);

            selectedOrder.ScheduleStatus =
                GetComboItem(cmbScheduleStatus);

            selectedOrder.Washing =
                cmbWashing.SelectedItem?.ToString() ?? "Not Started";

            selectedOrder.Drying =
                cmbDrying.SelectedItem?.ToString() ?? "Not Started";

            selectedOrder.Folding =
                cmbFolding.SelectedItem?.ToString() ?? "Not Started";

            selectedOrder.Packing =
                cmbPacking.SelectedItem?.ToString() ?? "Not Started";

            selectedOrder.Notes = txtNotes.Text;

            // Determine the current progress stage
            if (selectedOrder.Packing == "Current")
                selectedOrder.ProgressStage = "Packing";
            else if (selectedOrder.Folding == "Current")
                selectedOrder.ProgressStage = "Folding";
            else if (selectedOrder.Drying == "Current")
                selectedOrder.ProgressStage = "Drying";
            else if (selectedOrder.Washing == "Current")
                selectedOrder.ProgressStage = "Washing";
            else if (selectedOrder.Packing == "Completed")
                selectedOrder.ProgressStage = "Completed";
            else
                selectedOrder.ProgressStage = "Pending";

            ordersView.Refresh();

            MessageBox.Show(
                "Order status updated successfully!",
                "Update Status",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // UPLOAD PHOTO (FILE SELECTION ONLY)
        private void btnUploadPhoto_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (selectedOrder == null)
                return;

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Laundry Photo",
                Filter = "Image Files|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                selectedOrder.PhotoPath = dialog.FileName;

                MessageBox.Show(
                    "Photo selected successfully.",
                    "Upload Photo");
            }
        }

        // DISPATCH BUTTON PLACEHOLDER
        private void btnDispatch_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (selectedOrder == null)
                return;

            MessageBox.Show(
                "Dispatch functionality will be connected later.",
                "Dispatch");
        }
    }

    // SAMPLE ORDER MODEL
    public class StatusOrder
    {
        public string TrackingNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ServiceCode { get; set; } = "";
        public string ServiceName { get; set; } = "";

        public string ServiceStatus { get; set; } = "";
        public string ProgressStage { get; set; } = "";
        public string TurnaroundTime { get; set; } = "";
        public string ScheduleStatus { get; set; } = "";

        public string Washing { get; set; } = "Not Started";
        public string Drying { get; set; } = "Not Started";
        public string Folding { get; set; } = "Not Started";
        public string Packing { get; set; } = "Not Started";

        public string Notes { get; set; } = "";
        public string PhotoPath { get; set; } = "";
    }
}
