using System;
using UnityEngine;

/// <summary>
/// Manages heartbeat/ping system for detecting disconnections.
/// Sends periodic pings and tracks last pong received.
/// </summary>
public class HeartbeatManager : MonoBehaviour
{
    [Header("Heartbeat Settings")]
    public float pingInterval = 2f;        // Send ping every 2 seconds
    public float timeoutDuration = 10f;    // Disconnect if no pong for 10 seconds

    public Action OnConnectionTimeout;     // Triggered when connection times out

    private NetworkClient networkClient;
    private float lastPingTime;
    private float lastPongTime;
    private bool isRunning = false;

    public void Initialize(NetworkClient client)
    {
        networkClient = client;
        lastPongTime = Time.time;
        isRunning = true;

        // Subscribe to incoming messages
        networkClient.OnMessageReceived += HandleMessageReceived;
    }

    void Update()
    {
        if (!isRunning || networkClient == null)
            return;

        // Send periodic pings
        if (Time.time - lastPingTime >= pingInterval)
        {
            SendPing();
            lastPingTime = Time.time;
        }

        // Check for timeout
        if (Time.time - lastPongTime > timeoutDuration)
        {
            Debug.LogWarning("[HeartbeatManager] Connection timeout - no pong received");
            OnConnectionTimeout?.Invoke();
            Stop();
        }
    }

    private void SendPing()
    {
        PingMessage ping = new PingMessage(Time.time);
        networkClient.Send(ping);
    }

    private void HandleMessageReceived(INetworkMessage message)
    {
        if (message is PongMessage pong)
        {
            lastPongTime = Time.time;
            float rtt = Time.time - pong.timestamp;
            Debug.Log($"[HeartbeatManager] Pong received - RTT: {rtt * 1000:F1}ms");
            
            // Record RTT in statistics
            if (networkClient?.Statistics != null)
            {
                networkClient.Statistics.RecordRtt(rtt * 1000f); // Convert to milliseconds
            }
        }
    }

    public void Stop()
    {
        isRunning = false;
        if (networkClient != null)
        {
            networkClient.OnMessageReceived -= HandleMessageReceived;
        }
    }

    void OnDestroy()
    {
        Stop();
    }

    /// <summary>
    /// Returns time since last successful pong in seconds
    /// </summary>
    public float GetTimeSinceLastPong()
    {
        return Time.time - lastPongTime;
    }

    /// <summary>
    /// Returns true if connection appears healthy
    /// </summary>
    public bool IsConnectionHealthy()
    {
        return isRunning && (Time.time - lastPongTime) < timeoutDuration;
    }
    
    /// <summary>
    /// Get current RTT from statistics
    /// </summary>
    public float GetCurrentRtt()
    {
        return networkClient?.Statistics?.AverageRtt ?? 0f;
    }
    
    /// <summary>
    /// Get RTT jitter from statistics
    /// </summary>
    public float GetRttJitter()
    {
        return networkClient?.Statistics?.RttJitter ?? 0f;
    }
}
