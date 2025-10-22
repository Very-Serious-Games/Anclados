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

    public NetworkClient(ITransport transport, INetworkSerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
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
        byte[] data = _serializer.Serialize(message);
        _transport.SendToServer(data);
    }

    // Private Functions
    
    private void HandleConnected()
    {
        OnConnected?.Invoke();
    }

    private void HandleDisconnected()
    {
        OnDisconnected?.Invoke();
    }

    private void HandleDataReceived(byte[] data)
    {
        INetworkMessage message = _serializer.Deserialize<INetworkMessage>(data);
        OnMessageReceived?.Invoke(message);
    }
}
