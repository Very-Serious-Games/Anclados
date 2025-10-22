using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class UdpTransport : ITransport
{
    public event Action<int> OnClientConnected;

    // TODO: This action is not used because we don't have a way to know when a client disconnects :/ 
    // TODO: Implement a system that tells the Server that The Client is disconnecting (Heartbeats with pings and ACK).
    public event Action<int> OnClientDisconnected;
    public event Action<int, byte[]> OnDataReceived;

    public event Action OnConnectedToServer;
    public event Action OnDisconnectedFromServer;
    public event Action<byte[]> OnDataReceivedFromServer;

    private Socket socket;
    private CancellationTokenSource cts;

    // Server side variables
    private int nextConnectionId = 1;
    private readonly Dictionary<int, IPEndPoint> connectionsById = new Dictionary<int, IPEndPoint>();
    private readonly Dictionary<IPEndPoint, int> connectionsByEndPoint = new Dictionary<IPEndPoint, int>();

    // Client side variables
    private IPEndPoint serverEndPoint;
    bool connectedToServer = false;


    // --- Server Functions ---

    public void StartServer(int port)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            cts = new CancellationTokenSource();
            connectionsById.Clear();
            connectionsByEndPoint.Clear();

            Debug.Log($"[UdpTransport - Socket] Server started at port {port}");

            Task.Run(() => ListenServerAsync(cts.Token));            
        }
        catch (Exception e)
        {
            Debug.LogError($"[UdpTransport - Socket] Server start failed: {e.Message}");
        }
    }

    public void StopServer()
    {
        Debug.Log("[UdpTransport - Socket] Server stopping...");

        cts.Cancel();
        socket.Close();
        connectionsByEndPoint.Clear();
        connectionsById.Clear();
    }

    private async Task ListenServerAsync(CancellationToken token)
    {
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!token.IsCancellationRequested)
        {
            try
            {
                byte[] buffer = new byte[1024];
                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp);
                IPEndPoint clientEp = (IPEndPoint)result.RemoteEndPoint;

                if (!connectionsByEndPoint.TryGetValue(clientEp, out int connectionId))
                {
                    connectionId = nextConnectionId++;
                    connectionsById[connectionId] = clientEp;
                    connectionsByEndPoint[clientEp] = connectionId;

                    OnClientConnected?.Invoke(connectionId);
                }

                Array.Resize(ref buffer, result.ReceivedBytes);
                OnDataReceived?.Invoke(connectionId, buffer);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception e)
            {
                Debug.LogError($"[UdpTransport - Socket] Server receive error: {e.Message}");
            }
        }
    }

    public void Connect(string ip, int port)
    {
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            cts = new CancellationTokenSource();

            Debug.Log($"[UdpTransport - Socket] Client connecting to {ip}:{port}");

            Task.Run(() => ListenClientAsync(cts.Token));
        }
        catch (Exception e)
        {
            Debug.LogError($"[UdpTransport - Socket] Client connect failed: {e.Message}");
        }
    }

    public void Disconnect()
    {
        Debug.Log("[UdpTransport - Socket] Client disconnecting...");
        cts?.Cancel();
        socket?.Close();
        connectedToServer = false;

        OnDisconnectedFromServer?.Invoke();
    }

    private async Task ListenClientAsync(CancellationToken token)
    {
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!token.IsCancellationRequested)
        {
            try
            {
                byte[] buffer = new byte[1024];
                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp);

                if (result.RemoteEndPoint.Equals(serverEndPoint))
                {
                    if (!connectedToServer)
                    {
                        connectedToServer = true;
                        OnConnectedToServer?.Invoke();
                    }

                    Array.Resize(ref buffer, result.ReceivedBytes);
                    OnDataReceivedFromServer?.Invoke(buffer);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception e)
            {
                Debug.LogError($"[UdpTransport - Socket] Client receive error: {e.Message}");
            }
        }
    }

    public void SendToServer(byte[] data)
    {
        if (socket == null || serverEndPoint == null) return;
        
        socket.SendToAsync(data, SocketFlags.None, serverEndPoint);
    }

    public void SendToClient(int connectionId, byte[] data)
    {
        if (socket == null) return;

        if (connectionsById.TryGetValue(connectionId, out IPEndPoint endpoint))
        {
            socket.SendToAsync(data, SocketFlags.None, endpoint);
        }
    }
    
    public void Dispose()
    {
        cts?.Cancel();
        socket?.Close();
        socket?.Dispose();
        cts?.Dispose();
    }
}
