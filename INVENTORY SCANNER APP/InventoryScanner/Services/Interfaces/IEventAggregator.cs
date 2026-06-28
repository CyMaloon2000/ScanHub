using System;

namespace InventoryScanner.Services.Interfaces
{
    /// <summary>
    /// Lightweight publish/subscribe event aggregator for decoupled communication.
    /// </summary>
    public interface IEventAggregator
    {
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent eventData);
    }
}
