
using System.Collections.Generic;
using System.Threading.Tasks;
using MottoLaundry.Models;

namespace MottoLaundry.Repository
{
    public interface IOrderRepository
    {
        Task<int> AddOrderAsync(OrderModel order);

        Task<bool> TrackingNumberExistsAsync(
            string trackingNumber);

        Task<bool> ServiceCodeExistsAsync(
            string serviceCode);

        Task<List<string>> GetServiceCodesAsync();

        Task<List<OrderModel>> GetOrdersAsync();
    }
}
