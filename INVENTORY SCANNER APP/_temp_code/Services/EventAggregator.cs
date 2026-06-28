using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using InventoryScanner.Services.Interfaces;

namespace InventoryScanner.Services
{
    /// <summary>
    /// Thread-safe publish/subscribe event aggregator for decoupled communication.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
        private readonly object _lock = new();

        /// <inheritdoc/>
        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(eventType))
                    _subscribers[eventType] = new List<Delegate>();

                _subscribers[eventType].Add(handler);
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            lock (_lock)
            {
                if (_subscribers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        /// <inheritdoc/>
        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);
            List<Delegate>? handlersCopy;

            lock (_lock)
            {
                if (!_subscribers.TryGetValue(eventType, out var handlers))
                    return;

                handlersCopy = new List<Delegate>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EventAggregator error: {ex.Message}");
                }
            }
        }
    }
}
