using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MottoLaundry.Models
{
    public class OrderModel : INotifyPropertyChanged
    {

        //  Centralized Model: Shared Model that can be used by
        //  Dashboard, CustomerRecord, StatusMonitoring, and Report
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string property = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        private string _serviceStatus = "Not Started";
        private string _scheduleStatus = "On Schedule";
        private string _progressStage = "Pending";
        private bool _isDispatched;
        private string _notes = "";
        private string _photoPath = "";

        // Core fields (bound to AddOrderView)
        public string TrackingNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ServiceCode { get; set; } = "";
        public double Load { get; set; }

        // Management fields
        public int TurnaroundTime { get; set; } = 72;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ServiceStatus { get => _serviceStatus; set { _serviceStatus = value; Notify(); } }
        public string ScheduleStatus { get => _scheduleStatus; set { _scheduleStatus = value; Notify(); } }
        public string ProgressStage { get => _progressStage; set { _progressStage = value; Notify(); } }
        public bool IsDispatched { get => _isDispatched; set { _isDispatched = value; Notify(); } }
        public string Notes { get => _notes; set { _notes = value; Notify(); } }
        public string PhotoPath { get => _photoPath; set { _photoPath = value; Notify(); } }

        // 4 Granular Stages
        public StageProgress Washing { get; } = new StageProgress();
        public StageProgress Drying { get; } = new StageProgress();
        public StageProgress Folding { get; } = new StageProgress();
        public StageProgress Packing { get; } = new StageProgress();
    }

    public class StageProgress : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string property = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        private string _status = "Not Started";
        private string _employeeNameOrId = "";

        public string Status
        {
            get => _status;
            set { _status = value; Notify(); Notify(nameof(IsCompleted)); }
        }

        public string EmployeeNameOrId
        {
            get => _employeeNameOrId;
            set { _employeeNameOrId = value; Notify(); }
        }

        public bool IsCompleted => Status == "Completed";
    }
}