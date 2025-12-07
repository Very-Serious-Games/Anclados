using UnityEngine;

public enum ConnectionType
{
    Host,
    Client
}

public enum ServerType
{
    TCP,
    UDP
}

// Singleton
public class GameManager : MonoBehaviour
{
    // Public variables
    public NetworkServer gameServer = null;
    public NetworkClient gameClient = null;
    public NetworkServer chatServer = null;
    public NetworkClient chatClient = null;
    public ConnectionType connectionType;

    private string username;

    // -- Singleton Logic -- //

    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
    }

    void Update()
    {
        // Update network instances for packet batching
        gameServer?.Update();
        gameClient?.Update();
        chatServer?.Update();
        chatClient?.Update();
    }

    private NetworkServer CreateServerInstance(ServerType serverType, string serverName)
    {
        ITransport transport = serverType == ServerType.TCP ? (ITransport)new TcpTransport() : new UdpTransport();
        INetworkSerializer serializer = new JSONNetSerializer();
        
        NetworkServer server = new NetworkServer(transport, serializer);
        Debug.Log($"[GameManager] Created {serverType} server: {serverName}");
        
        return server;
    }

    public void CreateServer(ServerType serverType)
    {
        if (gameServer == null)
        {
            gameServer = CreateServerInstance(serverType, "Game");
            Debug.Log($"Created {serverType} game server.");
        }
        else if (chatServer == null)
        {
            chatServer = CreateServerInstance(serverType, "Chat");
            Debug.Log($"Created {serverType} chat server.");
        }
        else
        {
            Debug.LogWarning("Both gameServer and chatServer already exist.");
        }
    }

    private NetworkClient CreateClientInstance(ServerType serverType, string clientName)
    {
        ITransport transport = serverType == ServerType.TCP ? (ITransport)new TcpTransport() : new UdpTransport();
        INetworkSerializer serializer = new JSONNetSerializer();
        
        NetworkClient client = new NetworkClient(transport, serializer);
        Debug.Log($"[GameManager] Created {serverType} client: {clientName}");
        
        return client;
    }

    // TODO: Add a way to choose the client type created
    public void StartClient(ServerType serverType)
    {
        if (gameClient == null)
        {
            gameClient = CreateClientInstance(serverType, "Game");
            Debug.Log($"Created {serverType} game client.");
        }
        else if (chatClient == null)
        {
            chatClient = CreateClientInstance(serverType, "Chat");
            Debug.Log($"Created {serverType} chat client.");
        }
        else
        {
            Debug.LogWarning("Both gameClient and chatClient already exist.");
        }
    }

    public void SetUsername(string name)
    {
        username = name;
    }

    public string GetUsername()
    {
        return username;
    }

    private void OnApplicationQuit()
    {
        CleanupNetworking();
    }

    private void OnDestroy()
    {
        // Only cleanup if this is the singleton instance being destroyed
        if (instance == this)
        {
            // Don't call Destroy inside OnDestroy, just disconnect/stop
            CleanupNetworkingWithoutDestroy();
            instance = null;
        }
    }

    private void CleanupNetworking()
    {
        // Stop and cleanup servers
        if (gameServer != null)
        {
            gameServer.StopServer();
            gameServer = null;
        }

        if (chatServer != null)
        {
            chatServer.StopServer();
            chatServer = null;
        }

        // Disconnect and cleanup clients
        if (gameClient != null)
        {
            gameClient.Disconnect();
            gameClient = null;
        }

        if (chatClient != null)
        {
            chatClient.Disconnect();
            chatClient = null;
        }
    }

    private void CleanupNetworkingWithoutDestroy()
    {
        // Stop servers
        if (gameServer != null)
        {
            gameServer.StopServer();
            gameServer = null;
        }

        if (chatServer != null)
        {
            chatServer.StopServer();
            chatServer = null;
        }

        // Disconnect clients
        if (gameClient != null)
        {
            gameClient.Disconnect();
            gameClient = null;
        }

        if (chatClient != null)
        {
            chatClient.Disconnect();
            chatClient = null;
        }
    }
}