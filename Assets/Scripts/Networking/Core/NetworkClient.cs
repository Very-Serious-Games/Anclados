using UnityEngine;
using System;

public class NetworkClient
{
    // Events
    public Action OnConnected;
    public Action OnDisconnected;
    public Action<INetworkMessage> OnMessageReceived;

    // Dependencies
    private readonly ITransport _transport;
    private readonly INetworkSerializer _serializer;
    
    // Packet batching
    private PacketQueue _packetQueue;
    public bool enableBatching = true;
    public int maxMessagesPerPacket = 10;
    public float autoFlushInterval = 0.05f;

    public NetworkClient(ITransport transport, INetworkSerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
        
        // Initialize packet queue in constructor
        _packetQueue = new PacketQueue(
            packet => SendPacketImmediate(packet),
            maxMessagesPerPacket,
            autoFlushInterval
        );
    }

    public void Connect(string address, int port)
    {
        _transport.OnConnectedToServer += HandleConnected;
        _transport.OnDisconnectedFromServer += HandleDisconnected;
        _transport.OnDataReceivedFromServer += HandleDataReceived;

        _transport.Connect(address, port);
    }

    public void Disconnect()
    {
        _transport.Disconnect();

        _transport.OnConnectedToServer -= HandleConnected;
        _transport.OnDisconnectedFromServer -= HandleDisconnected;
        _transport.OnDataReceivedFromServer -= HandleDataReceived;
    }

    public void Send<T>(T message) where T : INetworkMessage
    {
        if (enableBatching)
        {
            _packetQueue.Enqueue(message);
        }
        else
        {
            byte[] data = _serializer.Serialize(message);
            _transport.SendToServer(data);
            
            // Track bandwidth for stats UI
            NetworkStatsUI statsUI = UnityEngine.Object.FindFirstObjectByType<NetworkStatsUI>();
            statsUI?.OnDataSent(data.Length);
        }
    }
    
    private void SendPacketImmediate(MessagePacket packet)
    {
        byte[] data = _serializer.Serialize(packet);
        _transport.SendToServer(data);
        
        // Track bandwidth for stats UI
        NetworkStatsUI statsUI = UnityEngine.Object.FindFirstObjectByType<NetworkStatsUI>();
        statsUI?.OnDataSent(data.Length);
    }

    // Private Functions
    
    private void HandleConnected()
    {
        OnConnected?.Invoke();
    }
    
    /// <summary>
    /// Send join message to announce presence to server (call this when entering game scene)
    /// </summary>
    public void SendJoinMessage()
    {
        string username = GameManager.Instance?.GetUsername() ?? "Guest";
        Send(new JoinMessage(username));
        Debug.Log($"[NetworkClient] Sent JoinMessage with username: {username}");
    }

    private void HandleDisconnected()
    {
        OnDisconnected?.Invoke();
    }

    private void HandleDataReceived(byte[] data)
    {
        // Track bandwidth for stats UI
        NetworkStatsUI statsUI = UnityEngine.Object.FindFirstObjectByType<NetworkStatsUI>();
        statsUI?.OnDataReceived(data.Length);
        
        INetworkMessage message = _serializer.Deserialize(data);
        
        // Unpack batched messages
        if (message is MessagePacket packet)
        {
            foreach (var unpackedMessage in packet.UnpackMessages())
            {
                OnMessageReceived?.Invoke(unpackedMessage);
            }
        }
        else
        {
            OnMessageReceived?.Invoke(message);
        }
    }
    
    public void Update()
    {
        // Update packet queue for timed flushing
        _packetQueue?.Update();
    }
}
