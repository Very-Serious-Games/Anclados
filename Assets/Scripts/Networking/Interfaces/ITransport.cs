using UnityEngine;
using System;

public interface ITransport
{
    // --- Server-Side Events ---
    event Action<int> OnClientConnected;
    event Action<int> OnClientDisconnected;
    event Action<int, byte[]> OnDataReceived;

    // --- Client-Side Events ---
    event Action OnConnectedToServer;
    event Action OnDisconnectedFromServer;
    event Action<byte[]> OnDataReceivedFromServer;

    // --- Functions ---
    void StartServer(int port);
    void StopServer();

    void Connect(string ip, int port);
    void Disconnect();

    void SendToServer(byte[] data);
    void SendToClient(int connectionId, byte[] data);
}
