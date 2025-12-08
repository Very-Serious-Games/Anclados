using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-side heartbeat monitor for detecting client disconnections.
/// Tracks last pong from each client and disconnects timed-out clients.
/// </summary>
public class ServerHeartbeatMonitor : MonoBehaviour
{
    [Header("Heartbeat Settings")]
    public float checkInterval = 1f;        // Check clients every second
    public float timeoutDuration = 10f;     // Disconnect if no pong for 10 seconds

    private NetworkServer networkServer;
    private float lastCheckTime;
    private bool isRunning = false;

    public void Initialize(NetworkServer server)
    {
        networkServer = server;
        isRunning = true;

        // Subscribe to incoming messages to handle pings
        networkServer.OnMessageReceived += HandleMessageReceived;
    }

    void Update()
    {
        if (!isRunning || networkServer == null)
            return;

        // Periodically check for timed-out clients
        if (Time.time - lastCheckTime >= checkInterval)
        {
            CheckForTimeouts();
            lastCheckTime = Time.time;
        }
    }

    private void HandleMessageReceived(Peer peer, INetworkMessage message)
    {
        // Respond to pings with pongs
        if (message is PingMessage ping)
        {
            peer.LastPongTime = Time.time;
            
            PongMessage pong = new PongMessage(ping.timestamp);
            networkServer.Send(peer, pong);
        }
    }

    private void CheckForTimeouts()
    {
        List<Peer> timedOutPeers = new List<Peer>();

        // Collect timed-out peers (can't modify dictionary during iteration)
        foreach (var kvp in networkServer.GetConnectedPeers())
        {
            Peer peer = kvp.Value;
            float timeSinceLastPong = peer.GetTimeSinceLastPong();

            if (timeSinceLastPong > timeoutDuration)
            {
                NetLog.ServerWarning($"Peer {peer.ConnectionId} timed out - no pong for {timeSinceLastPong:F1}s");
                timedOutPeers.Add(peer);
            }
        }

        // Force disconnect timed-out peers
        foreach (Peer peer in timedOutPeers)
        {
            NetLog.Server($"Forcing disconnect for peer {peer.ConnectionId}");
            
            // Get transport and force disconnect
            var transportField = networkServer.GetType().GetField("_transport", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ITransport transport = (ITransport)transportField?.GetValue(networkServer);
            
            if (transport is UdpTransport udp)
            {
                udp.ForceDisconnect(peer.ConnectionId);
            }
            else if (transport is TcpTransport tcp)
            {
                tcp.ForceDisconnect(peer.ConnectionId);
            }
        }
    }

    public void Stop()
    {
        isRunning = false;
        if (networkServer != null)
        {
            networkServer.OnMessageReceived -= HandleMessageReceived;
        }
    }

    void OnDestroy()
    {
        Stop();
    }
}
