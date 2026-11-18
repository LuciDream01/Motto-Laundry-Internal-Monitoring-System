
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MottoLaundry.Models;
using MottoLaundry.Repository;

namespace MottoLaundry.Services
{
    public class StatusMonitoringService
    {
        private readonly IOrderRepository _repository;

        private static readonly string[] StageNames =
        {
            "Washing",
            "Drying",
            "Folding",
            "Packing"
        };

        // =====================================================
        // SECTION 1: CONSTRUCTORS
        // =====================================================

        public StatusMonitoringService()
            : this(new OrderRepository())
        {
        }

        public StatusMonitoringService(
            IOrderRepository repository)
        {
            _repository = repository ??
                throw new ArgumentNullException(nameof(repository));
        }

        // =====================================================
        // SECTION 2: SCHEDULE STATUS
        // =====================================================

        public string CalculateScheduleStatus(OrderModel order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            DateTime deadline =
                order.CreatedAt.AddHours(order.TurnaroundTime);

            if (order.IsDispatched)
                return order.ScheduleStatus;

            return DateTime.Now > deadline
                ? "Delayed"
                : "On Schedule";
        }

        // =====================================================
        // SECTION 3: SERVICE STATUS
        // =====================================================

        public string CalculateServiceStatus(string[] statuses)
        {
            if (statuses == null || statuses.Length != 4)
            {
                throw new ArgumentException(
                    "Exactly four stages are required.");
            }

            string washing = statuses[0];
            string drying = statuses[1];
            string folding = statuses[2];
            string packing = statuses[3];

            if (packing == "Completed")
                return "Unclaimed";

            if (folding == "Completed")
                return "Completed";

            if (washing == "Current" ||
                washing == "Completed" ||
                drying == "Current" ||
                drying == "Completed" ||
                folding == "Current")
            {
                return "In Progress";
            }

            return "Not Started";
        }

        // =====================================================
        // SECTION 4: STAGE VALIDATION
        // =====================================================

        private static void ValidateStages(string[] statuses)
        {
            if (statuses == null || statuses.Length != 4)
            {
                throw new ArgumentException(
                    "Exactly four stages are required.");
            }

            int currentCount = 0;

            for (int i = 0; i < statuses.Length; i++)
            {
                string status = statuses[i];

                if (status != "Not Started" &&
                    status != "Current" &&
                    status != "Completed")
                {
                    throw new ArgumentException(
                        $"Invalid status for {StageNames[i]}.");
                }

                if (status == "Current")
                    currentCount++;

                if (i > 0 &&
                    status != "Not Started" &&
                    statuses[i - 1] != "Completed")
                {
                    throw new InvalidOperationException(
                        $"Complete {StageNames[i - 1]} " +
                        $"before starting {StageNames[i]}.");
                }
            }

            if (currentCount > 1)
            {
                throw new InvalidOperationException(
                    "Only one stage can be Current.");
            }
        }

        // =====================================================
        // SECTION 5: EMPLOYEE NAME LOOKUP
        // =====================================================

        private async Task<int?[]> ResolveEmployeeNamesAsync(
            string[] statuses,
            string[] employees)
        {
            if (employees == null || employees.Length != 4)
            {
                throw new ArgumentException(
                    "Four employee inputs are required.");
            }

            var employeeIds = new int?[4];

            for (int i = 0; i < 4; i++)
            {
                string name = employees[i]?.Trim() ?? "";

                if (statuses[i] == "Not Started")
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        throw new ArgumentException(
                            $"Clear the employee name for " +
                            $"{StageNames[i]} because this stage " +
                            "has not started.");
                    }

                    employeeIds[i] = null;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(
                        $"Employee Name is required for " +
                        $"{StageNames[i]}.");
                }

                if (name.Length > 100)
                {
                    throw new ArgumentException(
                        $"Employee Name for {StageNames[i]} " +
                        "cannot exceed 100 characters.");
                }

                int? employeeId =
                    await _repository.GetActiveEmployeeIdByNameAsync(
                        name);

                if (!employeeId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Employee '{name}' does not exist " +
                        "or is inactive.");
                }

                employeeIds[i] = employeeId.Value;
            }

            return employeeIds;
        }

        // =====================================================
        // SECTION 6: UPDATE ORDER
        // =====================================================

        public async Task UpdateAsync(
            OrderModel order,
            string[] statuses,
            string[] employees,
            string notes)
        {
            if (order == null)
            {
                throw new InvalidOperationException(
                    "Select an order first.");
            }

            if (order.IsDispatched)
            {
                throw new InvalidOperationException(
                    "Dispatched orders cannot be modified.");
            }

            statuses = statuses?
                .Select(x => x?.Trim() ?? "")
                .ToArray();

            ValidateStages(statuses);

            int?[] employeeIds =
                await ResolveEmployeeNamesAsync(
                    statuses, employees);

            if ((notes ?? "").Length > 500)
            {
                throw new ArgumentException(
                    "Notes cannot exceed 500 characters.");
            }

            string serviceStatus =
                CalculateServiceStatus(statuses);

            await _repository.UpdateOrderStatusAsync(
                order.TrackingNumber,
                statuses,
                employeeIds,
                serviceStatus,
                notes ?? "");

            StageProgress[] stages =
            {
                order.Washing,
                order.Drying,
                order.Folding,
                order.Packing
            };

            for (int i = 0; i < 4; i++)
            {
                stages[i].Status = statuses[i];

                stages[i].EmployeeNameOrId =
                    statuses[i] == "Not Started"
                        ? ""
                        : employees[i]?.Trim() ?? "";
            }

            order.ServiceStatus = serviceStatus;
            order.Notes = notes ?? "";

            Refresh(order);
        }

        // =====================================================
        // SECTION 7: REFRESH DISPLAY VALUES
        // =====================================================

        public void Refresh(OrderModel order)
        {
            if (order == null)
                return;

            order.ScheduleStatus =
                CalculateScheduleStatus(order);

            if (order.IsDispatched)
            {
                order.ServiceStatus = "Dispatched";
                order.ProgressStage = "Dispatched";
                return;
            }

            string washing = order.Washing.Status;
            string drying = order.Drying.Status;
            string folding = order.Folding.Status;
            string packing = order.Packing.Status;

            string[] statuses =
            {
                washing,
                drying,
                folding,
                packing
            };

            order.ServiceStatus =
                CalculateServiceStatus(statuses);

            // Packing completed:
            // Service Status = Unclaimed
            // Progress Stage = Packing
            if (packing == "Completed")
            {
                order.ProgressStage = "Packing";
                return;
            }

            // Packing underway:
            // Service Status = Completed
            // Progress Stage = Packing
            if (packing == "Current")
            {
                order.ProgressStage = "Packing";
                return;
            }

            // Folding completed:
            // Service Status = Completed
            // Progress Stage = Folding
            if (folding == "Completed")
            {
                order.ProgressStage = "Folding";
                return;
            }

            if (folding == "Current")
            {
                order.ProgressStage = "Folding";
                return;
            }

            if (drying == "Current")
            {
                order.ProgressStage = "Drying";
                return;
            }

            if (drying == "Completed")
            {
                order.ProgressStage = "Drying";
                return;
            }

            if (washing == "Current")
            {
                order.ProgressStage = "Washing";
                return;
            }

            if (washing == "Completed")
            {
                order.ProgressStage = "Washing";
                return;
            }

            order.ProgressStage = "Pending";
        }

        // =====================================================
        // SECTION 8: DISPATCH
        // =====================================================

        public async Task DispatchAsync(OrderModel order)
        {
            if (order == null)
            {
                throw new InvalidOperationException(
                    "Select an order first.");
            }

            if (order.IsDispatched)
            {
                throw new InvalidOperationException(
                    "Order already dispatched.");
            }

            if (!order.Washing.IsCompleted ||
                !order.Drying.IsCompleted ||
                !order.Folding.IsCompleted ||
                !order.Packing.IsCompleted)
            {
                throw new InvalidOperationException(
                    "Complete all four stages before dispatch.");
            }

            await _repository.DispatchOrderAsync(
                order.TrackingNumber);

            order.IsDispatched = true;
            order.ServiceStatus = "Dispatched";
            order.ProgressStage = "Dispatched";
        }

        // =====================================================
        // SECTION 9: PHOTO UPLOAD
        // =====================================================

        public async Task UploadPhotoAsync(
            OrderModel order,
            string sourcePath)
        {
            if (order == null)
            {
                throw new InvalidOperationException(
                    "Select an order first.");
            }

            if (order.IsDispatched)
            {
                throw new InvalidOperationException(
                    "Cannot upload to a dispatched order.");
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "Selected photo does not exist.");
            }

            string extension =
                Path.GetExtension(sourcePath).ToLowerInvariant();

            string[] allowed =
            {
                ".jpg", ".jpeg", ".png", ".bmp"
            };

            if (!allowed.Contains(extension))
            {
                throw new ArgumentException(
                    "Unsupported image format.");
            }

            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MottoLaundry",
                "OrderPhotos");

            Directory.CreateDirectory(folder);

            string filename =
                Guid.NewGuid().ToString("N") + extension;

            string destination =
                Path.Combine(folder, filename);

            File.Copy(sourcePath, destination);

            try
            {
                await _repository.AddOrderPhotoAsync(
                    order.TrackingNumber,
                    destination);

                order.PhotoPath = destination;
            }
            catch
            {
                if (File.Exists(destination))
                    File.Delete(destination);

                throw;
            }
        }
    }
}
