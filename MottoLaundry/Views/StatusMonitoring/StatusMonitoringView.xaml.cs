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
        private readonly ObservableCollection<StatusOrder> orders = new ObservableCollection<StatusOrder>();
        private ICollectionView ordersView;
        private StatusOrder selectedOrder;
        private bool loadingStages;

        public StatusMonitoringView()
        {
            InitializeComponent();
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            InitializeStageComboBoxes();
            LoadSampleOrders();
            ordersView = CollectionViewSource.GetDefaultView(orders);
            ordersView.Filter = FilterOrders;
            dgOrders.ItemsSource = ordersView;
        }

        private CheckBox[] StageChecks => new[] { chkWashing, chkDrying, chkFolding, chkPacking };
        private ComboBox[] StageCombos => new[] { cmbWashing, cmbDrying, cmbFolding, cmbPacking };
        private TextBox[] StageEmployees => new[] { txtWashingEmployee, txtDryingEmployee, txtFoldingEmployee, txtPackingEmployee };
        private static readonly string[] StageNames = { "Washing", "Drying", "Folding", "Packing" };

        private void InitializeStageComboBoxes()
        {
            foreach (ComboBox combo in StageCombos)
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
                ProgressStage = "Completed",
                TurnaroundTime = "48 Hours",
                ScheduleStatus = "Delayed",
                Washing = "Completed",
                Drying = "Completed",
                Folding = "Completed",
                Packing = "Completed"
            });
        }

        private bool FilterOrders(object item)
        {
            if (!(item is StatusOrder order)) return false;
            string search = txtSearch.Text.Trim();
            return string.IsNullOrWhiteSpace(search)
                || order.TrackingNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                || order.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || order.ServiceStatus.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ordersView?.Refresh();

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(dgOrders.SelectedItem is StatusOrder order)) return;
            selectedOrder = order;
            ShowUpdatePanel();
            LoadSelectedOrder(order);
        }

        private void ShowUpdatePanel()
        {
            OrdersColumn.Width = new GridLength(3, GridUnitType.Star);
            DetailsGapColumn.Width = new GridLength(15);
            DetailsColumn.Width = new GridLength(2, GridUnitType.Star);
            UpdateStatusPanel.Visibility = Visibility.Visible;
        }

        private void HideUpdatePanel()
        {
            UpdateStatusPanel.Visibility = Visibility.Collapsed;
            DetailsGapColumn.Width = new GridLength(0);
            DetailsColumn.Width = new GridLength(0);
            OrdersColumn.Width = new GridLength(1, GridUnitType.Star);
            selectedOrder = null;
            dgOrders.SelectedItem = null;
        }

        private void btnCloseDetails_Click(object sender, RoutedEventArgs e) => HideUpdatePanel();

        private void LoadSelectedOrder(StatusOrder order)
        {
            loadingStages = true;
            try
            {
                txtSelectedTracking.Text = order.TrackingNumber;
                txtSelectedCustomer.Text = order.CustomerName;
                txtSelectedService.Text = order.ServiceName;
                SelectComboItem(cmbScheduleStatus, order.ScheduleStatus);
                string[] values = { order.Washing, order.Drying, order.Folding, order.Packing };
                for (int i = 0; i < 4; i++) StageCombos[i].SelectedItem = values[i];
                txtWashingEmployee.Text = order.WashingEmployee;
                txtDryingEmployee.Text = order.DryingEmployee;
                txtFoldingEmployee.Text = order.FoldingEmployee;
                txtPackingEmployee.Text = order.PackingEmployee;
                txtNotes.Text = order.Notes;
                RefreshStageControls();
            }
            finally { loadingStages = false; }
        }

        private static void SelectComboItem(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
                if (item.Content?.ToString() == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
        }

        private static string GetComboItem(ComboBox combo) =>
            (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        private string StageValue(int index) => StageCombos[index].SelectedItem?.ToString() ?? "Not Started";

        private void StageCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (loadingStages || selectedOrder == null) return;
            int index = Array.IndexOf(StageChecks, sender);
            if (index < 0) return;
            loadingStages = true;
            try
            {
                if (StageChecks[index].IsChecked == true)
                {
                    if (StageValue(index) == "Not Started" && (index == 0 || StageValue(index - 1) == "Completed"))
                        StageCombos[index].SelectedItem = "Current";
                }
                else if (StageValue(index) == "Current")
                {
                    StageCombos[index].SelectedItem = "Not Started";
                }
                RefreshStageControls();
            }
            finally { loadingStages = false; }
        }

        private void StageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadingStages || selectedOrder == null) return;
            int index = Array.IndexOf(StageCombos, sender);
            if (index < 0) return;
            loadingStages = true;
            try
            {
                // A stage must be started with its checkbox before it can be completed.
                // Once a stage is completed, it cannot be reverted while a later stage has started.
                string status = StageValue(index);
                bool priorComplete = index == 0 || StageValue(index - 1) == "Completed";
                bool laterStarted = index < 3 && StageValue(index + 1) != "Not Started";
                if ((!priorComplete && status != "Not Started") ||
                    (laterStarted && status != "Completed") ||
                    (status == "Current" && StageChecks[index].IsChecked != true))
                {
                    StageCombos[index].SelectedItem = GetSavedStageValue(index);
                }
                RefreshStageControls();
            }
            finally { loadingStages = false; }
        }

        private string GetSavedStageValue(int index)
        {
            switch (index)
            {
                case 0: return selectedOrder.Washing;
                case 1: return selectedOrder.Drying;
                case 2: return selectedOrder.Folding;
                default: return selectedOrder.Packing;
            }
        }

        private void RefreshStageControls()
        {
            // A checked box means the stage has started (Current or Completed).
            // Only the first unfinished stage can be edited.
            for (int i = 0; i < 4; i++)
            {
                bool priorComplete = i == 0 || StageValue(i - 1) == "Completed";
                bool laterStarted = i < 3 && StageValue(i + 1) != "Not Started";
                string status = StageValue(i);
                StageChecks[i].IsChecked = status != "Not Started";
                StageChecks[i].IsEnabled = priorComplete && !laterStarted && status != "Completed";
                StageCombos[i].IsEnabled = priorComplete && status != "Not Started" && !laterStarted;
                StageEmployees[i].IsEnabled = status != "Not Started";
            }
            string serviceStatus = CalculateServiceStatus();
            SelectComboItem(cmbServiceStatus, serviceStatus);
            btnDispatch.IsEnabled = serviceStatus == "Completed" || serviceStatus == "Unclaimed";
        }

        private string CalculateServiceStatus()
        {
            if (selectedOrder?.ServiceStatus == "Dispatched") return "Dispatched";
            if (Enumerable.Range(0, 4).All(i => StageValue(i) == "Completed"))
                return selectedOrder?.ServiceStatus == "Unclaimed" ? "Unclaimed" : "Completed";
            return StageValue(0) == "Not Started" ? "Not Started" : "In Progress";
        }

        private void btnUpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            string[] stages = Enumerable.Range(0, 4).Select(StageValue).ToArray();
            for (int i = 1; i < 4; i++)
                if (stages[i] != "Not Started" && stages[i - 1] != "Completed")
                {
                    MessageBox.Show("Complete " + StageNames[i - 1] + " before starting " + StageNames[i] + ".", "Stage Order");
                    return;
                }

            selectedOrder.Washing = stages[0];
            selectedOrder.Drying = stages[1];
            selectedOrder.Folding = stages[2];
            selectedOrder.Packing = stages[3];
            selectedOrder.WashingEmployee = txtWashingEmployee.Text;
            selectedOrder.DryingEmployee = txtDryingEmployee.Text;
            selectedOrder.FoldingEmployee = txtFoldingEmployee.Text;
            selectedOrder.PackingEmployee = txtPackingEmployee.Text;
            selectedOrder.ScheduleStatus = GetComboItem(cmbScheduleStatus);
            selectedOrder.Notes = txtNotes.Text;
            selectedOrder.ServiceStatus = CalculateServiceStatus();
            int current = Array.FindIndex(stages, s => s == "Current");
            selectedOrder.ProgressStage = current >= 0 ? StageNames[current] :
                stages.All(s => s == "Completed") ? "Completed" :
                stages.All(s => s == "Not Started") ? "Pending" :
                StageNames[Array.FindIndex(stages, s => s == "Not Started")];
            ordersView.Refresh();
            RefreshStageControls();
            MessageBox.Show("Order status updated successfully!", "Update Status", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            var dialog = new OpenFileDialog { Title = "Select Laundry Photo", Filter = "Image Files|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                selectedOrder.PhotoPath = dialog.FileName;
                MessageBox.Show("Photo selected successfully.", "Upload Photo");
            }
        }

        private void btnDispatch_Click(object sender, RoutedEventArgs e)
        {
            if (selectedOrder == null) return;
            if (selectedOrder.ServiceStatus != "Completed" && selectedOrder.ServiceStatus != "Unclaimed")
            {
                MessageBox.Show("Complete and save all four stages before dispatching.", "Dispatch");
                return;
            }
            selectedOrder.ServiceStatus = "Dispatched";
            selectedOrder.ProgressStage = "Completed";
            ordersView.Refresh();
            RefreshStageControls();
            MessageBox.Show("Order dispatched successfully!", "Dispatch", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

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
        public string WashingEmployee { get; set; } = "";
        public string DryingEmployee { get; set; } = "";
        public string FoldingEmployee { get; set; } = "";
        public string PackingEmployee { get; set; } = "";
        public string Notes { get; set; } = "";
        public string PhotoPath { get; set; } = "";
    }
}
