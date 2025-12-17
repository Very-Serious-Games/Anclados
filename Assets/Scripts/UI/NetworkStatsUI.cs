using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays network statistics like RTT, connection health, packets per second
/// </summary>
public class NetworkStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rttText;
    public TextMeshProUGUI connectionStatusText;
    public TextMeshProUGUI packetStatsText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI bandwidthText;
    public TextMeshProUGUI ackStatsText;
    
    [Header("Settings")]
    public bool showStats = true;
    public float updateInterval = 0.5f;

    private HeartbeatManager heartbeat;
    private NetworkServer server;
    private NetworkClient client;
    private float lastUpdateTime;
    
    // Packet tracking
    private int packetsReceived = 0;
    private int packetsSent = 0;
    
    // Bandwidth tracking
    private int bytesSentThisSecond = 0;
    private int bytesReceivedThisSecond = 0;
    private int bytesSentPerSecond = 0;
    private int bytesReceivedPerSecond = 0;
    private float bandwidthTimer = 0f;

    void Start()
    {
        // Get network references from GameManager
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            server = gm.gameServer;
            client = gm.gameClient;
        }
        
        if (!showStats && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!showStats) return;
        
        // Try to find heartbeat if we don't have it yet
        if (heartbeat == null)
        {
            NetworkHeartbeatIntegration integration = FindFirstObjectByType<NetworkHeartbeatIntegration>();
            if (integration != null)
            {
                heartbeat = integration.GameClientHeartbeat;
                if (heartbeat != null)
                {
                    Debug.Log("[NetworkStatsUI] Found HeartbeatManager for RTT display");
                }
            }
        }
        
        // Update bandwidth stats every second
        bandwidthTimer += Time.deltaTime;
        if (bandwidthTimer >= 1.0f)
        {
            bytesSentPerSecond = bytesSentThisSecond;
            bytesReceivedPerSecond = bytesReceivedThisSecond;
            
            bytesSentThisSecond = 0;
            bytesReceivedThisSecond = 0;
            
            bandwidthTimer = 0f;
        }
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStats();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateStats()
    {
        // RTT (Round Trip Time)
        if (rttText != null && heartbeat != null)
        {
            float rtt = heartbeat.GetRtt();
            bool isHealthy = heartbeat.IsConnectionHealthy();
            
            Color color = isHealthy ? Color.green : Color.red;
            rttText.text = $"RTT: {rtt * 1000:F0}ms";
            rttText.color = color;
        }

        // Connection Status
        if (connectionStatusText != null)
        {
            GameManager gm = GameManager.Instance;
            string status = "Disconnected";
            Color statusColor = Color.red;

            if (gm.gameClient != null)
            {
                status = "Connected (Client)";
                statusColor = Color.green;
            }
            
            if (gm.gameServer != null)
            {
                int peerCount = gm.gameServer.GetPeerCount();
                status = $"Hosting ({peerCount} players)";
                statusColor = Color.cyan;
            }

            connectionStatusText.text = status;
            connectionStatusText.color = statusColor;
        }

        // Packet Stats
        if (packetStatsText != null)
        {
            float fps = 1f / Time.deltaTime;
            packetStatsText.text = $"FPS: {fps:F0}\nPackets: {packetsReceived}/s";
        }

        // Bandwidth Stats
        if (bandwidthText != null)
        {
            string sent = FormatBytes(bytesSentPerSecond);
            string received = FormatBytes(bytesReceivedPerSecond);
            string total = FormatBytes(bytesSentPerSecond + bytesReceivedPerSecond);
            bandwidthText.text = $"↑ {sent}/s\n↓ {received}/s\nTotal: {total}/s";
        }

        // ACK Stats (if server)
        if (ackStatsText != null && server != null && GameManager.Instance.connectionType == ConnectionType.Host)
        {
            string ackInfo = "ACK Stats:\n";
            foreach (var peer in server.GetConnectedPeers().Values)
            {
                if (peer.PlayerId != -1)
                {
                    int unacked = server.GetUnackedCount(peer.PlayerId);
                    int lastAck = server.GetLastAckedSequence(peer.PlayerId);
                    ackInfo += $"P{peer.PlayerId}: U={unacked} A={lastAck}\n";
                }
            }
            ackStatsText.text = ackInfo;
        }

        // Player Count
        if (playerCountText != null)
        {
            PlayerSpawnManager spawnMgr = FindFirstObjectByType<PlayerSpawnManager>();
            if (spawnMgr != null)
            {
                // Count spawned players
                int playerCount = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length;
                playerCountText.text = $"Players: {playerCount}";
            }
        }

        // Reset packet counters
        packetsReceived = 0;
    }

    /// <summary>
    /// Call this when a packet is received (for tracking)
    /// </summary>
    public void OnPacketReceived()
    {
        packetsReceived++;
    }

    /// <summary>
    /// Call this when a packet is sent (for tracking)
    /// </summary>
    public void OnPacketSent()
    {
        packetsSent++;
    }
    
    /// <summary>
    /// Call this when data is sent (for bandwidth tracking)
    /// </summary>
    public void OnDataSent(int byteCount)
    {
        bytesSentThisSecond += byteCount;
        packetsSent++;
    }
    
    /// <summary>
    /// Call this when data is received (for bandwidth tracking)
    /// </summary>
    public void OnDataReceived(int byteCount)
    {
        bytesReceivedThisSecond += byteCount;
        packetsReceived++;
    }

    /// <summary>
    /// Toggle stats visibility
    /// </summary>
    public void ToggleStats()
    {
        showStats = !showStats;
        gameObject.SetActive(showStats);
    }
    
    /// <summary>
    /// Format bytes into human-readable format
    /// </summary>
    private string FormatBytes(int bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:F2} KB";
        else
            return $"{bytes / (1024f * 1024f):F2} MB";
    }
}
