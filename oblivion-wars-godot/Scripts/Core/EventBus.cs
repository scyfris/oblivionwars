using Godot;
using System;
using System.Collections.Generic;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    public enum EventTiming { Immediate, NextFrame }

    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly Queue<Action> _deferredQueue = new();

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("EventBus: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        while (_deferredQueue.Count > 0)
        {
            _deferredQueue.Dequeue().Invoke();
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _subscribers[type] = list;
        }
        list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
        {
            list.Remove(handler);
        }
    }

    public void Raise<T>(T evt, EventTiming timing = EventTiming.Immediate) where T : struct, IGameEvent
    {
        if (timing == EventTiming.NextFrame)
        {
            _deferredQueue.Enqueue(() => InvokeSubscribers(evt));
            return;
        }

        InvokeSubscribers(evt);
    }

    private void InvokeSubscribers<T>(T evt) where T : struct, IGameEvent
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
            return;

        // Iterate a copy to allow subscribe/unsubscribe during dispatch
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (i < list.Count && list[i] is Action<T> handler)
            {
                handler.Invoke(evt);
            }
        }
    }
}
