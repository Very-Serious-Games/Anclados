using UnityEngine;
using System;

public class NetworkUpdateManager : MonoBehaviour
{
    [Header("Network Tick Rate")]
    [SerializeField] private float tickRate = 20f;
    
    private float tickInterval;
    private float timeSinceLastTick;
    
    public event Action OnNetworkTick;
    
    void Awake()
    {
        tickInterval = 1f / tickRate;
        timeSinceLastTick = 0f;
    }
    
    void Update()
    {
        timeSinceLastTick += Time.deltaTime;
        
        while (timeSinceLastTick >= tickInterval)
        {
            // Execute network tick
            OnNetworkTick?.Invoke();
            timeSinceLastTick -= tickInterval;
        }
    }
    
    public void SetTickRate(float newTickRate)
    {
        tickRate = Mathf.Max(1f, newTickRate);
        tickInterval = 1f / tickRate;
    }
    
    public float GetTickRate() => tickRate;
    public float GetTickInterval() => tickInterval;
}