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

    private NetworkServer CreateServerInstance(ServerType serverType, string serverName)
    {
        ITransport transport = serverType == ServerType.TCP ? (ITransport)new TcpTransport() : new UdpTransport();
        INetworkSerializer serializer = new JSONNetSerializer();
        
        GameObject serverObj = new GameObject($"NetworkServer_{serverName}");
        serverObj.transform.SetParent(this.transform);
        NetworkServer server = serverObj.AddComponent<NetworkServer>();
        
        // Use reflection to set readonly fields
        var transportField = typeof(NetworkServer).GetField("_transport", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var serializerField = typeof(NetworkServer).GetField("_serializer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        transportField?.SetValue(server, transport);
        serializerField?.SetValue(server, serializer);
        
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
        
        GameObject clientObj = new GameObject($"NetworkClient_{clientName}");
        clientObj.transform.SetParent(this.transform);
        NetworkClient client = clientObj.AddComponent<NetworkClient>();
        
        // Use reflection to set readonly fields
        var transportField = typeof(NetworkClient).GetField("_transport", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var serializerField = typeof(NetworkClient).GetField("_serializer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        transportField?.SetValue(client, transport);
        serializerField?.SetValue(client, serializer);
        
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
            if (gameServer.gameObject != null)
            {
                Destroy(gameServer.gameObject);
            }
            gameServer = null;
        }

        if (chatServer != null)
        {
            chatServer.StopServer();
            if (chatServer.gameObject != null)
            {
                Destroy(chatServer.gameObject);
            }
            chatServer = null;
        }

        // Disconnect and cleanup clients
        if (gameClient != null)
        {
            gameClient.Disconnect();
            if (gameClient.gameObject != null)
            {
                Destroy(gameClient.gameObject);
            }
            gameClient = null;
        }

        if (chatClient != null)
        {
            chatClient.Disconnect();
            if (chatClient.gameObject != null)
            {
                Destroy(chatClient.gameObject);
            }
            chatClient = null;
        }
    }

    private void CleanupNetworkingWithoutDestroy()
    {
        // Stop servers without destroying GameObjects
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

        // Disconnect clients without destroying GameObjects
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