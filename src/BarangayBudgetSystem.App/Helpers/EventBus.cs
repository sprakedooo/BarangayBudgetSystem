using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BarangayBudgetSystem.App.Helpers
{
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
        void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
        Task PublishAsync<TEvent>(TEvent eventData) where TEvent : class;
    }

    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _syncHandlers = new();
        private readonly Dictionary<Type, List<Delegate>> _asyncHandlers = new();
        private readonly object _lock = new();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_syncHandlers.ContainsKey(eventType))
                {
                    _syncHandlers[eventType] = new List<Delegate>();
                }
                _syncHandlers[eventType].Add(handler);
            }
        }

        public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_asyncHandlers.ContainsKey(eventType))
                {
                    _asyncHandlers[eventType] = new List<Delegate>();
                }
                _asyncHandlers[eventType].Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_syncHandlers.ContainsKey(eventType))
                {
                    _syncHandlers[eventType].Remove(handler);
                }
            }
        }

        public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_asyncHandlers.ContainsKey(eventType))
                {
                    _asyncHandlers[eventType].Remove(handler);
                }
            }
        }

        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            List<Delegate>? handlers;
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_syncHandlers.TryGetValue(eventType, out handlers))
                {
                    return;
                }
                handlers = new List<Delegate>(handlers);
            }

            foreach (var handler in handlers)
            {
                ((Action<TEvent>)handler)(eventData);
            }
        }

        public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : class
        {
            List<Delegate>? syncHandlers = null;
            List<Delegate>? asyncHandlers = null;

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_syncHandlers.TryGetValue(eventType, out var sh))
                {
                    syncHandlers = new List<Delegate>(sh);
                }
                if (_asyncHandlers.TryGetValue(eventType, out var ah))
                {
                    asyncHandlers = new List<Delegate>(ah);
                }
            }

            if (syncHandlers != null)
            {
                foreach (var handler in syncHandlers)
                {
                    ((Action<TEvent>)handler)(eventData);
                }
            }

            if (asyncHandlers != null)
            {
                foreach (var handler in asyncHandlers)
                {
                    await ((Func<TEvent, Task>)handler)(eventData);
                }
            }
        }
    }

    // Event classes
    public class FundUpdatedEvent
    {
        public int FundId { get; set; }
        public string? FundCode { get; set; }
        public decimal NewBalance { get; set; }
        public UpdateType UpdateType { get; set; }
    }

    public class TransactionCreatedEvent
    {
        public int TransactionId { get; set; }
        public string? TransactionNumber { get; set; }
        public int FundId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionType { get; set; }
    }

    public class TransactionStatusChangedEvent
    {
        public int TransactionId { get; set; }
        public string? TransactionNumber { get; set; }
        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }
    }

    public class UserLoggedInEvent
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
    }

    public class UserLoggedOutEvent
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
    }

    public class ReportGeneratedEvent
    {
        public int ReportId { get; set; }
        public string? ReportNumber { get; set; }
        public string? ReportType { get; set; }
        public string? FilePath { get; set; }
    }

    public class DashboardRefreshEvent
    {
        public bool RefreshFunds { get; set; }
        public bool RefreshTransactions { get; set; }
        public bool RefreshCharts { get; set; }
    }

    public class NavigationEvent
    {
        public string? ViewName { get; set; }
        public object? Parameter { get; set; }
    }

    public enum UpdateType
    {
        Created,
        Modified,
        Deleted
    }
}
