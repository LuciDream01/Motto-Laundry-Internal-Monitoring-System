
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MottoLaundry.Models;
using MottoLaundry.Services;

namespace MottoLaundry.ViewModel
{
    /// <summary>
    /// Coordinates Status Monitoring data and operations.
    /// </summary>
    public class StatusMonitoringViewModel
    {
        private readonly StatusMonitoringService _service;

        public ObservableCollection<OrderModel> Orders { get; }

        public OrderModel SelectedOrder { get; set; }

        // =====================================================
        // SECTION 1: CONSTRUCTOR
        // =====================================================

        public StatusMonitoringViewModel(
            ObservableCollection<OrderModel> orders)
        {
            Orders = orders ??
                throw new ArgumentNullException(nameof(orders));

            _service = new StatusMonitoringService();
        }

        // =====================================================
        // SECTION 2: UPDATE STATUS
        // =====================================================

        /// <summary>
        /// Validates and saves progress for the selected order.
        /// </summary>
        public async Task UpdateAsync(
            string[] statuses,
            string[] employees,
            string notes)
        {
            await _service.UpdateAsync(
                SelectedOrder,
                statuses,
                employees,
                notes);
        }

        // =====================================================
        // SECTION 3: DISPATCH
        // =====================================================

        /// <summary>
        /// Dispatches the selected order in SQL Server.
        /// </summary>
        public async Task DispatchAsync()
        {
            await _service.DispatchAsync(SelectedOrder);
        }

        // =====================================================
        // SECTION 4: PHOTO UPLOAD
        // =====================================================

        /// <summary>
        /// Saves the selected photo for the current order.
        /// </summary>
        public async Task UploadPhotoAsync(string filePath)
        {
            await _service.UploadPhotoAsync(
                SelectedOrder,
                filePath);
        }

        // =====================================================
        // SECTION 5: REFRESH
        // =====================================================

        /// <summary>
        /// Recalculates derived status information.
        /// </summary>
        public void RefreshOrder(OrderModel order)
        {
            _service.Refresh(order);
        }
    }
}
