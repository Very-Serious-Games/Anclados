using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        while (_queue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainThreadDispatcher] Action error: {e.Message}");
            }
        }
    }

    private static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("MainThreadDispatcher");
        _instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        EnsureInstance();
        _instance._queue.Enqueue(action);
    }
}
