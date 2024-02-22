using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleEvent", menuName = "Events/Simple Event")]
public class SimpleEvent : EventBase<Action>
{
    public void Send()
    {
        callback?.Invoke();
    }
}
