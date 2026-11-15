using System;
using System.Collections.ObjectModel;
using MottoLaundry.Models;
using MottoLaundry.Services;

namespace MottoLaundry.ViewModel
{
    public class StatusMonitoringViewModel
    {
        private readonly StatusMonitoringService service = new StatusMonitoringService();

        public ObservableCollection<OrderModel> Orders { get; }

        public OrderModel SelectedOrder { get; set; }

        // Constructor with sample data
        public StatusMonitoringViewModel()
            : this(new ObservableCollection<OrderModel>
            {
                new OrderModel
                {
                    TrackingNumber = "TRK-001",
                    CustomerName = "Juan Dela Cruz",
                    ServiceCode = "FULL",
                    ServiceStatus = "Not Started",
                    ProgressStage = "Not Started",
                    ScheduleStatus = "On Schedule"
                },
                new OrderModel
                {
                    TrackingNumber = "TRK-002",
                    CustomerName = "Maria Santos",
                    ServiceCode = "SELF",
                    ServiceStatus = "Not Started",
                    ProgressStage = "Not Started",
                    ScheduleStatus = "On Schedule"
                },
                new OrderModel
                {
                    TrackingNumber = "TRK-003",
                    CustomerName = "Pedro Reyes",
                    ServiceCode = "FULL",
                    ServiceStatus = "Not Started",
                    ProgressStage = "Not Started",
                    ScheduleStatus = "On Schedule"
                }
            })
        {
        }

        // Constructor for shared orders
        public StatusMonitoringViewModel(ObservableCollection<OrderModel> orders)
        {
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
        }

        public void Update(
            string[] statuses,
            string[] employees,
            string schedule,
            string notes)
        {
            service.Update(
                SelectedOrder,
                statuses,
                employees,
                schedule,
                notes);
        }

        public void Dispatch()
        {
            service.Dispatch(SelectedOrder);
        }
    }
}