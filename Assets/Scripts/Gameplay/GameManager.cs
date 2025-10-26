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
    public NetworkServer networkServer = null;
    public NetworkClient networkClient = null;
    public ConnectionType connectionType;

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

    // TODO: Add a way to choose the server type created
    public void CreateServer(ServerType serverType)
    {
        if (networkServer == null)
        {
            ITransport transport = serverType == ServerType.TCP ? new TcpTransport() : new UdpTransport();
            INetworkSerializer serializer = new JSONNetSerializer();

            networkServer = new NetworkServer(transport, serializer);
        }
        else
        {
            Debug.LogWarning("You are trying to create a server and a server is already created");
        }
    }

    // TODO: Add a way to choose the client type created
    public void StartClient(ServerType serverType)
    {
        if (networkClient == null)
        {
            ITransport transport = serverType == ServerType.TCP ? new TcpTransport() : new UdpTransport();
            INetworkSerializer serializer = new JSONNetSerializer();

            networkClient = new NetworkClient(transport, serializer);
        }
        else
        {
            Debug.LogWarning("You are trying to create a client and a client is already created");
        }
    }
}