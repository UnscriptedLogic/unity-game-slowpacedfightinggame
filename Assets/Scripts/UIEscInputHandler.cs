using System.Collections.Generic;
using UnityEngine;

public static class UIEscInputHandler
{
    public delegate void ListenerDelegate(out bool swallowEvent);

    private static List<ListenerDelegate> listeners = new List<ListenerDelegate>();

    public static void AddListener(ListenerDelegate listener)
    {
        listeners.Add(listener);
    }

    public static void RemoveListener(ListenerDelegate listener)
    {
        listeners.Remove(listener);
    }

    public static void Invoke()
    {
        Debug.Log(listeners.Count);

        bool swallowEvent = false;
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (listeners[i] == null)
            {
                listeners.RemoveAt(i);
                continue;
            }

            if (swallowEvent) continue;

            listeners[i]?.Invoke(out swallowEvent);
        }
    }
}