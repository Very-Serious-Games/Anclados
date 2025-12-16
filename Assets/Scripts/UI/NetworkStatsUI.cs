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
    
    [Header("Settings")]
    public bool showStats = true;
    public float updateInterval = 0.5f;

    private HeartbeatManager heartbeat;
    private float lastUpdateTime;
    private int packetsReceived = 0;
    private int packetsSent = 0;

    void Start()
    {
        // Try to find heartbeat manager
        heartbeat = FindFirstObjectByType<HeartbeatManager>();
        
        if (!showStats && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!showStats) return;
        
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
            float timeSincePong = heartbeat.GetTimeSinceLastPong();
            bool isHealthy = heartbeat.IsConnectionHealthy();
            
            Color color = isHealthy ? Color.green : Color.red;
            rttText.text = $"RTT: {timeSincePong * 1000:F0}ms";
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
    /// Toggle stats visibility
    /// </summary>
    public void ToggleStats()
    {
        showStats = !showStats;
        gameObject.SetActive(showStats);
    }
}
