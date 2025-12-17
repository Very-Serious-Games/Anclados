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
    
    // Statistics tracking
    private NetworkStatistics _statistics;
    public NetworkStatistics Statistics => _statistics;

    public NetworkClient(ITransport transport, INetworkSerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
        
        // Initialize statistics
        _statistics = new NetworkStatistics();
        
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
            _statistics.RecordPacketSent(data.Length, 1, false);
        }
    }
    
    private void SendPacketImmediate(MessagePacket packet)
    {
        byte[] data = _serializer.Serialize(packet);
        _transport.SendToServer(data);
        
        // Track statistics for batched packets
        int messageCount = packet.GetMessageCount();
        _statistics.RecordPacketSent(data.Length, messageCount, messageCount > 1);
    }

    // Private Functions
    
    private void HandleConnected()
    {
        _statistics.Reset();
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
        INetworkMessage message = _serializer.Deserialize(data);
        
        // Unpack batched messages
        if (message is MessagePacket packet)
        {
            int messageCount = packet.GetMessageCount();
            _statistics.RecordPacketReceived(data.Length, messageCount);
            
            foreach (var unpackedMessage in packet.UnpackMessages())
            {
                OnMessageReceived?.Invoke(unpackedMessage);
            }
        }
        else
        {
            _statistics.RecordPacketReceived(data.Length, 1);
            OnMessageReceived?.Invoke(message);
        }
    }
    
    public void Update()
    {
        // Update packet queue for timed flushing
        _packetQueue?.Update();
        
        // Update statistics calculations
        _statistics?.Update();
    }
}
