
using System.Collections.Generic;
using System.Threading.Tasks;
using MottoLaundry.Models;

namespace MottoLaundry.Repository
{
    /// <summary>
    /// Defines database operations for laundry orders.
    /// </summary>
    public interface IOrderRepository
    {
        // SECTION 1: ORDER OPERATIONS

        Task<int> AddOrderAsync(OrderModel order);

        Task<bool> TrackingNumberExistsAsync(
            string trackingNumber);

        Task<bool> ServiceCodeExistsAsync(
            string serviceCode);

        Task<List<string>> GetServiceCodesAsync();

        Task<List<OrderModel>> GetOrdersAsync();

        // SECTION 2: STATUS MONITORING

        Task UpdateOrderStatusAsync(
            string trackingNumber,
            string[] statuses,
            int?[] employeeIds,
            string serviceStatus,
            string notes);

        Task DispatchOrderAsync(string trackingNumber);

        Task AddOrderPhotoAsync(
            string trackingNumber,
            string photoPath);

        // SECTION 3: EMPLOYEE LOOKUP

        Task<bool> ActiveEmployeeExistsAsync(
            int employeeId);

        /// <summary>
        /// Resolves an active employee's exact name to
        /// their ID. Throws if the name is ambiguous.
        /// Returns null when no active employee matches.
        /// </summary>
        Task<int?> GetActiveEmployeeIdByNameAsync(
            string employeeName);

        /// <summary>
        /// Retrieves an employee's name by their ID.
        /// </summary>
        Task<string> GetEmployeeNameByIdAsync(
            int employeeId);
    }
}
