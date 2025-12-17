using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TcpTransport : ITransport
{
    public event Action<int> OnClientConnected;
    public event Action<int> OnClientDisconnected;
    public event Action<int, byte[]> OnDataReceived;

    public event Action OnConnectedToServer;
    public event Action OnDisconnectedFromServer;
    public event Action<byte[]> OnDataReceivedFromServer;

    private TcpListener listener;
    private TcpClient clientSocket;
    private CancellationTokenSource cts;

    // Server side variables
    private int nextConnectionId = 1;
    private readonly Dictionary<int, TcpClient> connectionsById = new Dictionary<int, TcpClient>();
    private readonly Dictionary<TcpClient, int> connectionsByClient = new Dictionary<TcpClient, int>();

    // Client side variables
    private bool connectedToServer = false;

    // --- Server Functions ---

    public void StartServer(int port)
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            cts = new CancellationTokenSource();
            connectionsById.Clear();
            connectionsByClient.Clear();

            MainThreadDispatcher.Enqueue(() => Debug.Log($"[TcpTransport - Socket] Server started at port {port}"));

            Task.Run(() => AcceptClientsAsync(cts.Token));
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Server start failed: {e.Message}"));
        }
    }

    public void StopServer()
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("[TcpTransport - Socket] Server stopping..."));

        cts?.Cancel();

        foreach (var client in connectionsById.Values)
        {
            client?.Close();
        }

        listener?.Stop();
        connectionsById.Clear();
        connectionsByClient.Clear();
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                
                int connectionId = nextConnectionId++;
                connectionsById[connectionId] = client;
                connectionsByClient[client] = connectionId;

                MainThreadDispatcher.Enqueue(() => OnClientConnected?.Invoke(connectionId));

                Task.Run(() => HandleClientAsync(client, connectionId, token));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Server accept error: {e.Message}"));
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, int connectionId, CancellationToken token)
    {
        NetworkStream stream = client.GetStream();

        try
        {
            while (!token.IsCancellationRequested && client.Connected)
            {
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, token);

                if (bytesRead == 0) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] buffer = new byte[messageLength];
                
                int totalRead = 0;
                while (totalRead < messageLength)
                {
                    bytesRead = await stream.ReadAsync(buffer, totalRead, messageLength - totalRead, token);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                if (totalRead == messageLength)
                {
                    byte[] dataCopy = buffer;
                    MainThreadDispatcher.Enqueue(() => OnDataReceived?.Invoke(connectionId, dataCopy));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Server receive error from client {connectionId}: {e.Message}"));
        }
        finally
        {
            connectionsById.Remove(connectionId);
            connectionsByClient.Remove(client);
            client?.Close();
            MainThreadDispatcher.Enqueue(() => OnClientDisconnected?.Invoke(connectionId));
        }
    }

    // --- Client Functions ---

    public void Connect(string ip, int port)
    {
        try
        {
            clientSocket = new TcpClient();
            cts = new CancellationTokenSource();

            MainThreadDispatcher.Enqueue(() => Debug.Log($"[TcpTransport - Socket] Client connecting to {ip}:{port}"));

            Task.Run(async () =>
            {
                try
                {
                    await clientSocket.ConnectAsync(ip, port);
                    connectedToServer = true;
                    MainThreadDispatcher.Enqueue(() => OnConnectedToServer?.Invoke());

                    await ListenClientAsync(cts.Token);
                }
                catch (Exception e)
                {
                    MainThreadDispatcher.Enqueue(() => 
                    {
                        Debug.LogError($"[TcpTransport - Socket] Client connect failed: {e.Message}");
                        OnDisconnectedFromServer?.Invoke();
                    });
                }
            });
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Client connect initialization failed: {e.Message}"));
        }
    }

    public void Disconnect()
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("[TcpTransport - Socket] Client disconnecting..."));
        cts?.Cancel();
        clientSocket?.Close();
        connectedToServer = false;

        MainThreadDispatcher.Enqueue(() => OnDisconnectedFromServer?.Invoke());
    }

    private async Task ListenClientAsync(CancellationToken token)
    {
        NetworkStream stream = clientSocket.GetStream();

        try
        {
            while (!token.IsCancellationRequested && clientSocket.Connected)
            {
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, token);

                if (bytesRead == 0) break;

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] buffer = new byte[messageLength];
                
                int totalRead = 0;
                while (totalRead < messageLength)
                {
                    bytesRead = await stream.ReadAsync(buffer, totalRead, messageLength - totalRead, token);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                if (totalRead == messageLength)
                {
                    byte[] dataCopy = buffer;
                    MainThreadDispatcher.Enqueue(() => OnDataReceivedFromServer?.Invoke(dataCopy));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Client receive error: {e.Message}"));
        }
        finally
        {
            connectedToServer = false;
            MainThreadDispatcher.Enqueue(() => OnDisconnectedFromServer?.Invoke());
        }
    }

    public void SendToServer(byte[] data)
    {
        if (clientSocket == null || !clientSocket.Connected) return;

        try
        {
            NetworkStream stream = clientSocket.GetStream();
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
            
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Send to server error: {e.Message}"));
        }
    }

    public void SendToClient(int connectionId, byte[] data)
    {
        if (!connectionsById.TryGetValue(connectionId, out TcpClient client)) return;
        if (!client.Connected) return;

        try
        {
            NetworkStream stream = client.GetStream();
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
            
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError($"[TcpTransport - Socket] Send to client {connectionId} error: {e.Message}"));
        }
    }
    
    /// <summary>
    /// Force disconnect a client
    /// </summary>
    public void ForceDisconnect(int connectionId)
    {
        if (connectionsById.TryGetValue(connectionId, out TcpClient client))
        {
            connectionsByClient.Remove(client);
            connectionsById.Remove(connectionId);
            
            client.Close();
            
            MainThreadDispatcher.Enqueue(() => {
                Debug.Log($"[TcpTransport - Socket] Forcefully disconnected client {connectionId}");
                OnClientDisconnected?.Invoke(connectionId);
            });
        }
    }

    public void Dispose()
    {
        cts?.Cancel();
        clientSocket?.Close();
        clientSocket?.Dispose();
        listener?.Stop();
        
        foreach (var client in connectionsById.Values)
        {
            client?.Close();
            client?.Dispose();
        }
        
        connectionsById.Clear();
        connectionsByClient.Clear();
        cts?.Dispose();
    }
}