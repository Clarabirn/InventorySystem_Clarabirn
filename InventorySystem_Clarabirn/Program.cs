// MainViewModel.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using InventoryApp.Model;

namespace InventoryApp
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public Inventory Inventory { get; }
        public OrderBook OrderBook { get; }
        private Order? _selectedQueued;
        public Order? SelectedQueued { get => _selectedQueued; set { _selectedQueued = value; Raise(); } }

        public decimal TotalRevenue => OrderBook.TotalRevenue();

        public ICommand ProcessNextOrderCommand { get; }

        public MainViewModel()
        {
            Inventory = new Inventory();
            SeedInventoryAndOrders(out var book);
            OrderBook = book;

            ProcessNextOrderCommand = new RelayCommand(
                _ =>
                {
                    OrderBook.ProcessNextOrder();
                    Raise(nameof(TotalRevenue));
                },
                _ => OrderBook.QueuedOrders.Count > 0
            );

            // Re-eval CanExecute når køen ændres
            OrderBook.QueuedOrders.CollectionChanged += (_, __) =>
            {
                (ProcessNextOrderCommand as RelayCommand)!.RaiseCanExecuteChanged();
                Raise(nameof(TotalRevenue));
            };
            OrderBook.ProcessedOrders.CollectionChanged += (_, __) => Raise(nameof(TotalRevenue));
        }

        private void SeedInventoryAndOrders(out OrderBook book)
        {
            var bolts = new UnitItem("Bolt M8", 2.50m, 0.012m);
            var nuts  = new UnitItem("Nut M8", 1.10m, 0.006m);
            var cable = new BulkItem("Cable", 15.00m, "m");
            var flour = new BulkItem("Flour", 4.20m, "kg");

            Inventory.Set(bolts, 120);
            Inventory.Set(nuts,  200);
            Inventory.Set(cable, 50);   // meter
            Inventory.Set(flour, 80);   // kg

            book = new OrderBook(Inventory);

            var alice = new Customer("Alice");
            var o1 = new Order(); o1.OrderLines.Add(new OrderLine(bolts, 10)); o1.OrderLines.Add(new OrderLine(nuts, 10));
            var o2 = new Order(); o2.OrderLines.Add(new OrderLine(cable, 7.5m));
            var o3 = new Order(); o3.OrderLines.Add(new OrderLine(flour, 12.0m)); o3.OrderLines.Add(new OrderLine(bolts, 4));

            alice.CreateOrder(book, o1);
            alice.CreateOrder(book, o2);
            alice.CreateOrder(book, o3);
        }
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
