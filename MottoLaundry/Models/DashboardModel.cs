using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Models
{
    public class DashboardModel
    {
        public string TrackingNumber { get; set; }
        public string CustomerName { get; set; }
        public string ServiceCode { get; set; }
        public double Load { get; set; }
        public string ServiceStatus { get; set; }
        public string ProgressStage { get; set; }
        public int TurnAroundTime { get; set; }
        public string ScheduleStatus { get; set; }
    }
}
