using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkServer
{
    // Public NetworkServer Events
    public Action OnServerStarted;
    public Action OnServerStopped;
    public Action<Peer> OnPlayerConnected;
    public Action<Peer> OnPlayerDisconnected;

    // Generic NetworkServer Event for any message type
    public Action<Peer, INetworkMessage> OnMessageReceived;

    // Dependency References
    private readonly ITransport _transport;
    private readonly INetworkSerializer _serializer;

    // Connected Peers
    private Dictionary<int, Peer> _connectedPeers;
    
    // Packet batching per peer
    private Dictionary<int, PacketQueue> _peerQueues;
    
    // Packet Batching Settings
    public bool enableBatching = true;
    public int maxMessagesPerPacket = 10;
    public float autoFlushInterval = 0.05f;

    public NetworkServer(ITransport transport, INetworkSerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
        
        // Initialize dictionaries in constructor
        _connectedPeers = new Dictionary<int, Peer>();
        _peerQueues = new Dictionary<int, PacketQueue>();
    }

    public void StartServer(int port)
    {
        _transport.OnClientConnected += HandleClientConnected;
        _transport.OnClientDisconnected += HandleClientDisconnected;
        _transport.OnDataReceived += HandleDataReceived;

        // Start the transport server
        _transport.StartServer(port);
        OnServerStarted?.Invoke();
    }

    public void StopServer()
    {
        _transport.StopServer();
        OnServerStopped?.Invoke();
        _connectedPeers.Clear();
        _peerQueues.Clear();

        _transport.OnClientConnected -= HandleClientConnected;
        _transport.OnClientDisconnected -= HandleClientDisconnected;
        _transport.OnDataReceived -= HandleDataReceived;
    }

    public void Send<T>(Peer peer, T message) where T : INetworkMessage
    {
        if (enableBatching && _peerQueues.TryGetValue(peer.ConnectionId, out PacketQueue queue))
        {
            queue.Enqueue(message);
        }
        else
        {
            byte[] data = _serializer.Serialize(message);
            _transport.SendToClient(peer.ConnectionId, data);
        }
    }
    
    private void SendPacketImmediate(int connectionId, MessagePacket packet)
    {
        byte[] data = _serializer.Serialize(packet);
        _transport.SendToClient(connectionId, data);
    }

    public void Broadcast<T>(T message, Peer excludePeer = null) where T : INetworkMessage
    {
        byte[] data = _serializer.Serialize(message);
        foreach (var peer in _connectedPeers.Values)
        {
            if (excludePeer != null && peer.ConnectionId == excludePeer.ConnectionId)
                continue;

            _transport.SendToClient(peer.ConnectionId, data);
        }
    }

    // ---------- Private Functions ------------- //

    private void HandleClientConnected(int connectionId)
    {
        // Ensure dictionaries are initialized
        if (_connectedPeers == null) _connectedPeers = new Dictionary<int, Peer>();
        if (_peerQueues == null) _peerQueues = new Dictionary<int, PacketQueue>();
        
        Peer newPeer = new Peer(connectionId);
        _connectedPeers.Add(connectionId, newPeer);
        
        // Create packet queue for this peer
        _peerQueues[connectionId] = new PacketQueue(
            packet => SendPacketImmediate(connectionId, packet),
            maxMessagesPerPacket,
            autoFlushInterval
        );
        
        OnPlayerConnected?.Invoke(newPeer);

        Debug.Log($"Client connected: {connectionId}");
    }

    private void HandleClientDisconnected(int connectionId)
    {
        if (_connectedPeers == null) return;
        
        if (_connectedPeers.TryGetValue(connectionId, out Peer peer))
        {
            _connectedPeers.Remove(connectionId);
            
            // Flush and remove queue
            if (_peerQueues.TryGetValue(connectionId, out PacketQueue queue))
            {
                queue.Flush();
                _peerQueues.Remove(connectionId);
            }
            
            OnPlayerDisconnected?.Invoke(peer);
        }
    }

    private void HandleDataReceived(int connectionId, byte[] data)
    {
        if (_connectedPeers == null || _serializer == null) return;
        
        if (_connectedPeers.TryGetValue(connectionId, out Peer peer))
        {
            INetworkMessage message = _serializer.Deserialize(data);
            
            // Unpack batched messages
            if (message is MessagePacket packet)
            {
                foreach (var unpackedMessage in packet.UnpackMessages())
                {
                    OnMessageReceived?.Invoke(peer, unpackedMessage);
                }
            }
            else
            {
                OnMessageReceived?.Invoke(peer, message);
            }
        }
    }
    
    public void Update()
    {
        // Update all packet queues for timed flushing
        if (_peerQueues != null)
        {
            foreach (var queue in _peerQueues.Values)
            {
                queue?.Update();
            }
        }
    }
    
    // ---------- Public Utility Methods ------------- //
    
    /// <summary>
    /// Get all connected peers (read-only)
    /// </summary>
    public IReadOnlyDictionary<int, Peer> GetConnectedPeers()
    {
        return _connectedPeers ?? new Dictionary<int, Peer>();
    }
    
    /// <summary>
    /// Get peer by connection ID
    /// </summary>
    public Peer GetPeer(int connectionId)
    {
        if (_connectedPeers == null) return null;
        _connectedPeers.TryGetValue(connectionId, out Peer peer);
        return peer;
    }
    
    /// <summary>
    /// Get peer by player ID
    /// </summary>
    public Peer GetPeerByPlayerId(int playerId)
    {
        if (_connectedPeers == null) return null;
        
        foreach (var peer in _connectedPeers.Values)
        {
            if (peer.PlayerId == playerId)
                return peer;
        }
        return null;
    }
    
    /// <summary>
    /// Get count of connected peers
    /// </summary>
    public int GetPeerCount()
    {
        return _connectedPeers?.Count ?? 0;
    }
}
