using System;
using System.Collections.Generic;

namespace Arkanoid.Core
{
    /// <summary>
    /// Слабосвязанная шина событий. Подписки нужно снимать (Dispose / Unsubscribe), иначе утечки.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Подписаться на событие типа T.</summary>
        IDisposable Subscribe<T>(Action<T> handler);

        /// <summary>Опубликовать событие.</summary>
        void Publish<T>(T eventData);

        /// <summary>Снять конкретный обработчик.</summary>
        void Unsubscribe<T>(Action<T> handler);

        /// <summary>Очистить все подписки (смена сцены / teardown).</summary>
        void Clear();
    }

    /// <summary>
    /// Реализация Event Bus без аллокаций на Publish при отсутствии подписчиков.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>(32);

        /// <inheritdoc />
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(4);
                _subscribers[type] = list;
            }

            list.Add(handler);
            return new Subscription(() => Unsubscribe(handler));
        }

        /// <inheritdoc />
        public void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list) || list.Count == 0)
            {
                return;
            }

            // Копия на случай отписки во время Publish
            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] is Action<T> action)
                {
                    action.Invoke(eventData);
                }
            }
        }

        /// <inheritdoc />
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                _subscribers.Remove(type);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _subscribers.Clear();
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            private bool _isDisposed;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
