
using System;
using System.Threading.Tasks;
using MottoLaundry.Models;
using MottoLaundry.Repository;

namespace MottoLaundry.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _repository;
        private readonly TrackingNumberService _trackingNumberService;
        private readonly ServiceCodeService _serviceCodeService;

        private bool _serviceCodesLoaded;

        public OrderService(
            IOrderRepository repository,
            TrackingNumberService trackingNumberService,
            ServiceCodeService serviceCodeService)
        {
            _repository = repository;
            _trackingNumberService = trackingNumberService;
            _serviceCodeService = serviceCodeService;
        }

        public async Task LoadServiceCodesAsync()
        {
            var codes = await _repository.GetServiceCodesAsync();

            _serviceCodeService.LoadServiceCode(codes);

            _serviceCodesLoaded = true;
        }

        public async Task<int> AddOrderAsync(OrderModel order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            // STEP 1: Normalize input
            order.TrackingNumber =
                (order.TrackingNumber ?? "").Trim().ToUpperInvariant();

            order.CustomerName =
                (order.CustomerName ?? "").Trim();

            order.ServiceCode =
                (order.ServiceCode ?? "").Trim().ToUpperInvariant();

            // STEP 2: Validate tracking format
            if (!_trackingNumberService.isValidFormat(
                order.TrackingNumber))
            {
                throw new ArgumentException(
                    "Tracking number must follow TRK-### format.");
            }

            // STEP 3: Validate customer name
            if (string.IsNullOrWhiteSpace(order.CustomerName))
            {
                throw new ArgumentException(
                    "Customer name is required.");
            }

            // STEP 4: Validate load
            if (double.IsNaN(order.Load) ||
                double.IsInfinity(order.Load) ||
                order.Load <= 0)
            {
                throw new ArgumentException(
                    "Load must be greater than zero.");
            }

            // STEP 5: Load service codes if not yet loaded
            if (!_serviceCodesLoaded)
            {
                await LoadServiceCodesAsync();
            }

            // STEP 6: Validate service code
            if (!_serviceCodeService.IsValidServiceCode(
                order.ServiceCode))
            {
                throw new ArgumentException(
                    "Invalid service code.");
            }

            // STEP 7: Check duplicate tracking number
            bool trackingExists =
                await _repository.TrackingNumberExistsAsync(
                    order.TrackingNumber);

            if (trackingExists)
            {
                throw new InvalidOperationException(
                    "Tracking number already exists.");
            }

            // STEP 8: System-defined properties
            order.ServiceStatus = "Not Started";
            order.ScheduleStatus = "On Schedule";
            order.ProgressStage = "Pending";
            order.IsDispatched = false;
            order.CreatedAt = DateTime.Now;

            // STEP 9: Insert into SQL Server
            return await _repository.AddOrderAsync(order);
        }
    }
}
