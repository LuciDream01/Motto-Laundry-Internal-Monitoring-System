
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MottoLaundry.Models;
using MottoLaundry.Repository;
using MottoLaundry.Services;

namespace MottoLaundry.ViewModel
{
    public class CustomerRecordViewModel : INotifyPropertyChanged
    {
        // Dependencies
        private readonly OrderService _orderService;
        private readonly IOrderRepository _repository;

        private string _trackingNumber = string.Empty;
        private string _customerName = string.Empty;
        private string _serviceCode = string.Empty;
        private double _load;
        private bool _isSaving;

        public ObservableCollection<OrderModel> Orders { get; }
            = new ObservableCollection<OrderModel>();

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
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand SaveCommand { get; }

        // Existing parameterless constructor preserved
        public CustomerRecordViewModel()
        {
            _repository = new OrderRepository();

            var trackingService = new TrackingNumberService();
            var serviceCodeService = new ServiceCodeService();

            _orderService = new OrderService(
                _repository,
                trackingService,
                serviceCodeService);

            SaveCommand = new RelayCommand(
                async () => await ExecuteSaveAsync(),
                CanSave);

            // Load existing orders asynchronously
            _ = LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                var existingOrders =
                    await _repository.GetOrdersAsync();

                Orders.Clear();

                foreach (var order in existingOrders)
                {
                    Orders.Add(order);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to load orders:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSaveAsync()
        {
            if (_isSaving)
                return;

            _isSaving = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // STEP 1: Create order from user input
                var newOrder = new OrderModel
                {
                    TrackingNumber = TrackingNumber,
                    CustomerName = CustomerName,
                    ServiceCode = ServiceCode,
                    Load = Load
                };

                // STEP 2: Validate and save to SQL Server
                int orderId =
                    await _orderService.AddOrderAsync(newOrder);

                // STEP 3: Refresh records from database
                var savedOrders =
                    await _repository.GetOrdersAsync();

                Orders.Clear();

                foreach (var order in savedOrders)
                {
                    Orders.Add(order);
                }

                // STEP 4: Notify user
                MessageBox.Show(
                    $"Order successfully saved!\n" +
                    $"Order ID: {orderId}\n" +
                    $"Tracking Number: {newOrder.TrackingNumber}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // STEP 5: Reset form
                TrackingNumber = string.Empty;
                CustomerName = string.Empty;
                ServiceCode = string.Empty;
                Load = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to save order:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isSaving = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanSave()
        {
            return !_isSaving
                && !string.IsNullOrWhiteSpace(TrackingNumber)
                && !string.IsNullOrWhiteSpace(CustomerName)
                && !string.IsNullOrWhiteSpace(ServiceCode)
                && Load > 0
                && !double.IsNaN(Load)
                && !double.IsInfinity(Load);
        }

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    // Existing RelayCommand preserved
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
