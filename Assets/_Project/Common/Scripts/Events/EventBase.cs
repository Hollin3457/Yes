using System;
using UnityEngine;

public class EventBase<T> : ScriptableObject
     where T : Delegate
{
    protected T callback;

    public void Register(T action)
    {
        callback = Delegate.Combine(callback, action) as T;
    }

    public void Unregister(T action)
    {
        callback = Delegate.Remove(callback, action) as T;
    }
}

public class EventWith1Param<T> : EventBase<Action<T>>
{
    public void Send(T arg)
    {
        callback?.Invoke(arg);
    }
}

public class EventWith2Params<T1, T2> : EventBase<Action<T1, T2>>
{
    public void Send(T1 arg1, T2 arg2)
    {
        callback?.Invoke(arg1, arg2);
    }
}
