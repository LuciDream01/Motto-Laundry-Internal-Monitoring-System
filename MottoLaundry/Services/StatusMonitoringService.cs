using System;
using MottoLaundry.Models;

namespace MottoLaundry.Services
{
    public class StatusMonitoringService
    {
        public void Update(OrderModel order, string[] statuses, string[] employees, string schedule, string notes)
        {
            if (order == null) throw new Exception("Select an order first.");
            if (order.IsDispatched) throw new Exception("This order has already been dispatched.");
            if (statuses == null || statuses.Length != 4 || employees == null || employees.Length != 4)
                throw new Exception("Four stages are required.");

            for (int i = 0; i < 4; i++)
            {
                // Ensure null-safety on incoming status strings
                string status = statuses[i]?.Trim() ?? "";

                if (status != "Not Started" && status != "Current" && status != "Completed")
                    throw new Exception("Choose a valid stage status.");

                if (status != "Not Started" && string.IsNullOrWhiteSpace(employees[i]))
                    throw new Exception("Enter an employee name or ID for stage " + (i + 1) + ".");

                // FIXED: Changed i1 to (i - 1)
                if (i > 0 && status != "Not Started" && statuses[i - 1]?.Trim() != "Completed")
                    throw new Exception("Complete the previous stage first.");
            }

            int currentCount = 0;
            foreach (string status in statuses)
                if (status?.Trim() == "Current") currentCount++;
            if (currentCount > 1) throw new Exception("Only one stage can be Current.");

            StageProgress[] stages = { order.Washing, order.Drying, order.Folding, order.Packing };
            for (int i = 0; i < 4; i++)
            {
                stages[i].Status = statuses[i].Trim();
                stages[i].EmployeeNameOrId = employees[i]?.Trim() ?? "";
            }

            order.ScheduleStatus = schedule;
            order.Notes = notes ?? "";
            Refresh(order);
        }

        public void Refresh(OrderModel order)
        {
            // 72-Hour Turnaround Rule Check
            if ((DateTime.Now - order.CreatedAt).TotalHours > order.TurnaroundTime)
            {
                order.ScheduleStatus = "Delayed";
            }

            if (order.IsDispatched)
            {
                order.ServiceStatus = "Dispatched";
                order.ProgressStage = "Dispatched";
                return;
            }

            // Packing stage reached/completed -> Unclaimed
            if (order.Packing.Status == "Current" || order.Packing.IsCompleted)
            {
                order.ServiceStatus = "Unclaimed";
                order.ProgressStage = "Packing";
                return;
            }

            // Folding completed -> Completed
            if (order.Folding.IsCompleted)
            {
                order.ServiceStatus = "Completed";
                order.ProgressStage = "Folding";
                return;
            }

            StageProgress[] stages = { order.Washing, order.Drying, order.Folding, order.Packing };
            string[] names = { "Washing", "Drying", "Folding", "Packing" };
            order.ServiceStatus = "Not Started";
            order.ProgressStage = "Pending";

            for (int i = 0; i < 4; i++)
            {
                if (stages[i].Status == "Current")
                {
                    order.ServiceStatus = "In Progress";
                    order.ProgressStage = names[i];
                    return;
                }
                if (stages[i].IsCompleted)
                {
                    order.ServiceStatus = "In Progress";
                    order.ProgressStage = i < 3 ? names[i + 1] + " Pending" : "Completed";
                }
            }
        }

        public void Dispatch(OrderModel order)
        {
            if (order == null) throw new Exception("Select an order first.");
            if (order.IsDispatched) throw new Exception("Order already dispatched.");
            if (order.ServiceStatus != "Completed" && order.ServiceStatus != "Unclaimed")
                throw new Exception("Complete Folding or Packing before dispatch.");

            order.IsDispatched = true;
            Refresh(order);
        }
    }
}