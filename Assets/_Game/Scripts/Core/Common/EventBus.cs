using System;
using System.Collections.Generic;

namespace _Game.Scripts.Core.Common
{
    public interface IEvent
    {
    }

    public sealed class EventBus : IDisposable
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public IDisposable Subscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;

            return new Subscription<T>(this, handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
                _handlers.Remove(type);
            else
                _handlers[type] = remaining;
        }

        public void Trigger<T>(T evt) where T : IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var handler) && handler is Action<T> action)
                action(evt);
        }

        public void Trigger<T>() where T : IEvent, new() => Trigger(new T());

        public void Dispose() => _handlers.Clear();

        private sealed class Subscription<T> : IDisposable where T : IEvent
        {
            private EventBus _bus;
            private Action<T> _handler;

            public Subscription(EventBus bus, Action<T> handler)
            {
                _bus = bus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_bus == null) return;
                _bus.Unsubscribe(_handler);
                _bus = null;
                _handler = null;
            }
        }
    }
}