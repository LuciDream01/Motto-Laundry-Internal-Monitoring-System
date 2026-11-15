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
        private string _trackingNumber = string.Empty;
        private string _customerName = string.Empty;
        private string _serviceCode = string.Empty;
        private double _load;

        public ObservableCollection<OrderModel> Orders { get; } = new ObservableCollection<OrderModel>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TrackingNumber
        {
            get => _trackingNumber;
            set { if (_trackingNumber != value) { _trackingNumber = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        public string CustomerName
        {
            get => _customerName;
            set { if (_customerName != value) { _customerName = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }
        }

        public string ServiceCode
        {
            get => _serviceCode;
            set { if (_serviceCode != value) { _serviceCode = value; OnPropertyChanged(); } }
        }

        public double Load
        {
            get => _load;
            set { if (_load != value) { _load = value; OnPropertyChanged(); } }
        }

        public ICommand SaveCommand { get; }

        public CustomerRecordViewModel()
        {
            SaveCommand = new RelayCommand(ExecuteSave, CanSave);

            // Sample mockup data for testing
            Orders.Add(new OrderModel { TrackingNumber = "TRK-001", CustomerName = "Juan Dela Cruz", ServiceCode = "FULL", Load = 8.5 });
            Orders.Add(new OrderModel { TrackingNumber = "TRK-002", CustomerName = "Maria Santos", ServiceCode = "SELF", Load = 5.0 });
        }

        private void ExecuteSave()
        {
            var newOrder = new OrderModel
            {
                TrackingNumber = TrackingNumber.Trim(),
                CustomerName = CustomerName.Trim(),
                ServiceCode = ServiceCode.Trim(),
                Load = Load,
                CreatedAt = DateTime.Now,
                ServiceStatus = "Not Started",
                ProgressStage = "Pending",
                ScheduleStatus = "On Schedule"
            };

            Orders.Add(newOrder);

            MessageBox.Show($"Successfully Added New Order!\nTracking: {newOrder.TrackingNumber}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reset Form Fields
            TrackingNumber = string.Empty;
            CustomerName = string.Empty;
            ServiceCode = string.Empty;
            Load = 0;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(TrackingNumber) && !string.IsNullOrWhiteSpace(CustomerName);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }
}