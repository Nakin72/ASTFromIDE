// AstroEditor.Core/Common/WeakEvent.cs
// WeakEvent pattern для предотвращения утечек памяти через события
// ✅ P1-2: Реализация слабых событий

namespace AstroEditor.Core.Common;

/// <summary>
/// Слабое событие для предотвращения утечек памяти.
/// ✅ P1-2: Использует WeakReference для подписчиков.
/// </summary>
public class WeakEvent
{
    private readonly List<WeakReference<Delegate>> _handlers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Добавить подписчика.
    /// </summary>
    public void Subscribe(Delegate handler)
    {
        lock (_lock)
        {
            // Очищаем мёртвые ссылки перед добавлением
            CleanupDeadReferences();
            _handlers.Add(new WeakReference<Delegate>(handler));
        }
    }

    /// <summary>
    /// Удалить подписчика.
    /// </summary>
    public void Unsubscribe(Delegate handler)
    {
        lock (_lock)
        {
            var toRemove = new List<WeakReference<Delegate>>();
            foreach (var weakRef in _handlers)
            {
                if (weakRef.TryGetTarget(out var target) && target == handler)
                {
                    toRemove.Add(weakRef);
                }
            }
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    /// <summary>
    /// Вызвать всех подписчиков.
    /// </summary>
    public void Raise(params object?[] args)
    {
        List<Delegate> handlersToInvoke;
        lock (_lock)
        {
            handlersToInvoke = new List<Delegate>();
            foreach (var weakRef in _handlers)
            {
                if (weakRef.TryGetTarget(out var target))
                {
                    handlersToInvoke.Add(target);
                }
            }
            // Асинхронно очищаем мёртвые ссылки
            Task.Run(CleanupDeadReferences);
        }

        foreach (var handler in handlersToInvoke)
        {
            try
            {
                handler.DynamicInvoke(args);
            }
            catch
            {
                // Игнорируем ошибки в подписчиках
            }
        }
    }

    /// <summary>
    /// Очистить мёртвые ссылки.
    /// </summary>
    private void CleanupDeadReferences()
    {
        lock (_lock)
        {
            var toRemove = _handlers.Where(wr => !wr.TryGetTarget(out _)).ToList();
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    /// <summary>
    /// Количество активных подписчиков.
    /// </summary>
    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _handlers.Count(wr => wr.TryGetTarget(out _));
            }
        }
    }
}

/// <summary>
/// Слабое событие с типизированными аргументами.
/// ✅ P1-2:Generic версия WeakEvent.
/// </summary>
public class WeakEvent<TArgs> where TArgs : class
{
    private readonly List<WeakReference<Action<TArgs>>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe(Action<TArgs> handler)
    {
        lock (_lock)
        {
            CleanupDeadReferences();
            _handlers.Add(new WeakReference<Action<TArgs>>(handler));
        }
    }

    public void Unsubscribe(Action<TArgs> handler)
    {
        lock (_lock)
        {
            var toRemove = _handlers
                .Where(wr => wr.TryGetTarget(out var target) && target == handler)
                .ToList();
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    public void Raise(TArgs args)
    {
        List<Action<TArgs>> handlersToInvoke;
        lock (_lock)
        {
            handlersToInvoke = new List<Action<TArgs>>();
            foreach (var weakRef in _handlers)
            {
                if (weakRef.TryGetTarget(out var target))
                {
                    handlersToInvoke.Add(target);
                }
            }
            Task.Run(CleanupDeadReferences);
        }

        foreach (var handler in handlersToInvoke)
        {
            try
            {
                handler(args);
            }
            catch
            {
                // Игнорируем ошибки в подписчиках
            }
        }
    }

    private void CleanupDeadReferences()
    {
        lock (_lock)
        {
            var toRemove = _handlers.Where(wr => !wr.TryGetTarget(out _)).ToList();
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _handlers.Count(wr => wr.TryGetTarget(out _));
            }
        }
    }
}

/// <summary>
/// WeakEvent с двумя аргументами.
/// </summary>
public class WeakEvent<TArg1, TArg2> where TArg1 : class where TArg2 : class
{
    private readonly List<WeakReference<Action<TArg1, TArg2>>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe(Action<TArg1, TArg2> handler)
    {
        lock (_lock)
        {
            CleanupDeadReferences();
            _handlers.Add(new WeakReference<Action<TArg1, TArg2>>(handler));
        }
    }

    public void Unsubscribe(Action<TArg1, TArg2> handler)
    {
        lock (_lock)
        {
            var toRemove = _handlers
                .Where(wr => wr.TryGetTarget(out var target) && target == handler)
                .ToList();
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    public void Raise(TArg1 arg1, TArg2 arg2)
    {
        List<Action<TArg1, TArg2>> handlersToInvoke;
        lock (_lock)
        {
            handlersToInvoke = new List<Action<TArg1, TArg2>>();
            foreach (var weakRef in _handlers)
            {
                if (weakRef.TryGetTarget(out var target))
                {
                    handlersToInvoke.Add(target);
                }
            }
            Task.Run(CleanupDeadReferences);
        }

        foreach (var handler in handlersToInvoke)
        {
            try
            {
                handler(arg1, arg2);
            }
            catch
            {
                // Игнорируем ошибки в подписчиках
            }
        }
    }

    private void CleanupDeadReferences()
    {
        lock (_lock)
        {
            var toRemove = _handlers.Where(wr => !wr.TryGetTarget(out _)).ToList();
            foreach (var weakRef in toRemove)
            {
                _handlers.Remove(weakRef);
            }
        }
    }

    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _handlers.Count(wr => wr.TryGetTarget(out _));
            }
        }
    }
}
