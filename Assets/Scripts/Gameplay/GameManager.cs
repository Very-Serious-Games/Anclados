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

    private NetworkServer CreateServerInstance(ServerType serverType)
    {
        ITransport transport = serverType == ServerType.TCP ? (ITransport)new TcpTransport() : new UdpTransport();
        INetworkSerializer serializer = new JSONNetSerializer();
        return new NetworkServer(transport, serializer);
    }

    public void CreateServer(ServerType serverType)
    {
        if (gameServer == null)
        {
            gameServer = CreateServerInstance(serverType);
            Debug.Log($"Created {serverType} game server.");
        }
        else if (chatServer == null)
        {
            chatServer = CreateServerInstance(serverType);
            Debug.Log($"Created {serverType} chat server.");
        }
        else
        {
            Debug.LogWarning("Both gameServer and chatServer already exist.");
        }
    }

    private NetworkClient CreateClientInstance(ServerType serverType)
    {
        ITransport transport = serverType == ServerType.TCP ? (ITransport)new TcpTransport() : new UdpTransport();
        INetworkSerializer serializer = new JSONNetSerializer();
        return new NetworkClient(transport, serializer);
    }

    // TODO: Add a way to choose the client type created
    public void StartClient(ServerType serverType)
    {
        if (gameClient == null)
        {
            gameClient = CreateClientInstance(serverType);
            Debug.Log($"Created {serverType} game client.");
        }
        else if (chatClient == null)
        {
            chatClient = CreateClientInstance(serverType);
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
}