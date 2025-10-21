using System;
using System.Collections.Generic;

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
    private Dictionary<int, Peer> _connectedPeers = new Dictionary<int, Peer>();

    public NetworkServer(ITransport transport, INetworkSerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
    }

    public void Start(int port)
    {
        _transport.OnClientConnected += HandleClientConnected;
        _transport.OnClientDisconnected += HandleClientDisconnected;
        _transport.OnDataReceived += HandleDataReceived;

        // Start the transport server
        _transport.StartServer(port);
        OnServerStarted?.Invoke();
    }

    public void Stop()
    {
        _transport.StopServer();
        OnServerStopped?.Invoke();
        _connectedPeers.Clear();

        _transport.OnClientConnected -= HandleClientConnected;
        _transport.OnClientDisconnected -= HandleClientDisconnected;
        _transport.OnDataReceived -= HandleDataReceived;
    }

    public void Send<T>(Peer peer, T message) where T : INetworkMessage
    {
        byte[] data = _serializer.Serialize(message);
        _transport.SendToClient(peer.ConnectionId, data);
    }

    public void Broadcast<T>(T message) where T : INetworkMessage
    {
        byte[] data = _serializer.Serialize(message);
        foreach (var peer in _connectedPeers.Values)
        {
            _transport.SendToClient(peer.ConnectionId, data);
        }
    }

    // ---------- Private Functions ------------- //

    private void HandleClientConnected(int connectionId)
    {
        Peer newPeer = new Peer(connectionId);
        _connectedPeers.Add(connectionId, newPeer);
        
        OnPlayerConnected?.Invoke(newPeer);
    }

    private void HandleClientDisconnected(int connectionId)
    {
        if (_connectedPeers.TryGetValue(connectionId, out Peer peer))
        {
            _connectedPeers.Remove(connectionId);
            
            OnPlayerDisconnected?.Invoke(peer);
        }
    }

    private void HandleDataReceived(int connectionId, byte[] data)
    {
        if (_connectedPeers.TryGetValue(connectionId, out Peer peer))
        {
            // This could be improved to be more useful using messageIDs to know the type
            INetworkMessage message = _serializer.Deserialize<INetworkMessage>(data);

            OnMessageReceived?.Invoke(peer, message);
        }
    }
}
