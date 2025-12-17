using UnityEngine;

/// <summary>
/// Example integration of heartbeat and packet batching systems.
/// Add this component to your GameManager or a network-aware GameObject.
/// </summary>
public class NetworkHeartbeatIntegration : MonoBehaviour
{
    private HeartbeatManager gameClientHeartbeat;
    private HeartbeatManager chatClientHeartbeat;
    private ServerHeartbeatMonitor gameServerMonitor;
    private ServerHeartbeatMonitor chatServerMonitor;
    
    public HeartbeatManager GameClientHeartbeat => gameClientHeartbeat;

    void Start()
    {
        // Wait a frame to ensure GameManager is initialized
        Invoke(nameof(InitializeHeartbeats), 0.1f);
    }

    private void InitializeHeartbeats()
    {
        GameManager gm = GameManager.Instance;

        // Setup client-side heartbeats (sends pings)
        if (gm.gameClient != null)
        {
            GameObject heartbeatObj = new GameObject("GameClient_Heartbeat");
            heartbeatObj.transform.SetParent(this.transform);
            gameClientHeartbeat = heartbeatObj.AddComponent<HeartbeatManager>();
            gameClientHeartbeat.Initialize(gm.gameClient);
            gameClientHeartbeat.OnConnectionTimeout += () => HandleClientTimeout("Game");
            
            Debug.Log("[NetworkHeartbeatIntegration] Game client heartbeat initialized");
        }

        if (gm.chatClient != null)
        {
            GameObject heartbeatObj = new GameObject("ChatClient_Heartbeat");
            heartbeatObj.transform.SetParent(this.transform);
            chatClientHeartbeat = heartbeatObj.AddComponent<HeartbeatManager>();
            chatClientHeartbeat.Initialize(gm.chatClient);
            chatClientHeartbeat.OnConnectionTimeout += () => HandleClientTimeout("Chat");
            
            Debug.Log("[NetworkHeartbeatIntegration] Chat client heartbeat initialized");
        }

        // Setup server-side heartbeat monitors (responds to pings, detects timeouts)
        if (gm.gameServer != null)
        {
            GameObject monitorObj = new GameObject("GameServer_HeartbeatMonitor");
            monitorObj.transform.SetParent(this.transform);
            gameServerMonitor = monitorObj.AddComponent<ServerHeartbeatMonitor>();
            gameServerMonitor.Initialize(gm.gameServer);
            
            Debug.Log("[NetworkHeartbeatIntegration] Game server monitor initialized");
        }

        if (gm.chatServer != null)
        {
            GameObject monitorObj = new GameObject("ChatServer_HeartbeatMonitor");
            monitorObj.transform.SetParent(this.transform);
            chatServerMonitor = monitorObj.AddComponent<ServerHeartbeatMonitor>();
            chatServerMonitor.Initialize(gm.chatServer);
            
            Debug.Log("[NetworkHeartbeatIntegration] Chat server monitor initialized");
        }
    }

    private void HandleClientTimeout(string serverName)
    {
        Debug.LogError($"[NetworkHeartbeatIntegration] {serverName} server connection lost!");
        
        // Handle reconnection or return to menu
        // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu Scene");
    }

    void OnDestroy()
    {
        // Cleanup
        if (gameClientHeartbeat != null) gameClientHeartbeat.Stop();
        if (chatClientHeartbeat != null) chatClientHeartbeat.Stop();
        if (gameServerMonitor != null) gameServerMonitor.Stop();
        if (chatServerMonitor != null) chatServerMonitor.Stop();
    }
}
