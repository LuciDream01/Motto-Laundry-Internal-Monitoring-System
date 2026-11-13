
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MottoLaundry.Models;

namespace MottoLaundry.ViewModel
{
    public class CustomerRecordViewModel : INotifyPropertyChanged
    {
        // PRIVATE FIELDS
        private string _trackingNumber = string.Empty;
        private string _customerName = string.Empty;
        private string _serviceCode = string.Empty;
        private double _load;

        private int _turnAroundTime = 72;
        private string _serviceStatus = "Not Started";
        private string _progressStage = "Pending";
        private string _scheduleStatus = "On Schedule";

        // COLLECTIONS
        public ObservableCollection<DashboardModel> DashboardData
        { get; } = new ObservableCollection<DashboardModel>();

        public ObservableCollection<CustomerRecordModel> CustomerRecordData
        { get; } = new ObservableCollection<CustomerRecordModel>();

        // PROPERTY CHANGED EVENT
        public event PropertyChangedEventHandler? PropertyChanged;

        // TRACKING NUMBER
        public string TrackingNumber
        {
            get => _trackingNumber;
            set
            {
                if (_trackingNumber != value)
                {
                    _trackingNumber = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // CUSTOMER NAME
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // SERVICE CODE
        public string ServiceCode
        {
            get => _serviceCode;
            set
            {
                if (_serviceCode != value)
                {
                    _serviceCode = value;
                    OnPropertyChanged();
                }
            }
        }

        // LOAD
        public double Load
        {
            get => _load;
            set
            {
                if (_load != value)
                {
                    _load = value;
                    OnPropertyChanged();
                }
            }
        }

        // TURNAROUND TIME
        public int TurnAroundTime
        {
            get => _turnAroundTime;
            set
            {
                if (_turnAroundTime != value)
                {
                    _turnAroundTime = value;
                    OnPropertyChanged();
                }
            }
        }

        // SERVICE STATUS
        public string ServiceStatus
        {
            get => _serviceStatus;
            set
            {
                if (_serviceStatus != value)
                {
                    _serviceStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // PROGRESS STAGE
        public string ProgressStage
        {
            get => _progressStage;
            set
            {
                if (_progressStage != value)
                {
                    _progressStage = value;
                    OnPropertyChanged();
                }
            }
        }

        // SCHEDULE STATUS
        public string ScheduleStatus
        {
            get => _scheduleStatus;
            set
            {
                if (_scheduleStatus != value)
                {
                    _scheduleStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // SAVE COMMAND
        public ICommand SaveCommand { get; }

        public CustomerRecordViewModel()
        {
            SaveCommand = new RelayCommand(ExecuteSave, CanSave);
        }

        private void ExecuteSave()
        {
            // SAVE COMPLETE ORDER FOR DASHBOARD
            var completeOrder = new DashboardModel
            {
                TrackingNumber = TrackingNumber,
                CustomerName = CustomerName,
                ServiceCode = ServiceCode,
                Load = Load,
                ServiceStatus = ServiceStatus,
                ProgressStage = ProgressStage,
                TurnAroundTime = TurnAroundTime,
                ScheduleStatus = ScheduleStatus
            };

            DashboardData.Add(completeOrder);

            // SAVE BASIC RECORD FOR CUSTOMER RECORDS
            var basicRecord = new CustomerRecordModel
            {
                TrackingNumber = TrackingNumber,
                CustomerName = CustomerName,
                ServiceCode = ServiceCode,
                Load = Load
            };

            CustomerRecordData.Add(basicRecord);

            MessageBox.Show(
                "Successfully Added New Order!",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            // RESET FORM
            TrackingNumber = string.Empty;
            CustomerName = string.Empty;
            ServiceCode = string.Empty;
            Load = 0;
            TurnAroundTime = 72;
            ServiceStatus = "Not Started";
            ProgressStage = "Pending";
            ScheduleStatus = "On Schedule";
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(TrackingNumber)
                && !string.IsNullOrWhiteSpace(CustomerName);
        }

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }
    }

    // RELAY COMMAND
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(
            Action execute,
            Func<bool>? canExecute = null)
        {
            _execute = execute
                ?? throw new ArgumentNullException(nameof(execute));

            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}
