using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace MottoLaundry.ViewModel
{
    public class CustomerRecordViewModel : INotifyPropertyChanged
    {
        private string _trackingNumber;
        private string _customerName;
        private string _serviceCode;
        private double _load;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TrackingNumber    
        {
            get => _trackingNumber;
            set
            {
                if (_trackingNumber != value)
                {
                    _trackingNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public ICommand SaveCommand { get; }

        public CustomerRecordViewModel()
        {
            // Pass the method to run when clicked, and an optional condition for when the button is enabled
            SaveCommand = new RelayCommand(ExecuteSave, CanSave);
        }

        private void ExecuteSave()
        {
            // 1. Create your Model from the ViewModel inputs
            // var record = new CustomerRecord 
            // { 
            //     TrackingNumber = this.TrackingNumber,
            //     CustomerName = this.CustomerName,
            //     ServiceCode = this.ServiceCode,
            //     Load = this.Load
            // };

            // 2. Pass 'record' to your SQL Server repository/service here

            MessageBox.Show($"Tracking Number: {TrackingNumber}\nCustomer Name:{CustomerName}\nService Code:{ServiceCode}\nLoad:{Load}");

            TrackingNumber = string.Empty;
            CustomerName = string.Empty;
            ServiceCode = string.Empty;
            Load = 0;
        }

        private bool CanSave()
        {
            // Button stays disabled until TrackingNumber and CustomerName are filled in
            return !string.IsNullOrWhiteSpace(TrackingNumber) &&
                   !string.IsNullOrWhiteSpace(CustomerName);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Standard helper class so ICommand works with methods
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