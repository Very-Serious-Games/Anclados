using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays comprehensive network statistics including RTT, bandwidth, packet stats, and more
/// </summary>
public class NetworkStatsUI : MonoBehaviour
{
    [Header("UI References - Connection")]
    public TextMeshProUGUI connectionStatusText;
    public TextMeshProUGUI uptimeText;
    public TextMeshProUGUI playerCountText;
    
    [Header("UI References - Latency")]
    public TextMeshProUGUI rttText;
    public TextMeshProUGUI jitterText;
    
    [Header("UI References - Bandwidth")]
    public TextMeshProUGUI uploadBandwidthText;
    public TextMeshProUGUI downloadBandwidthText;
    public TextMeshProUGUI totalTrafficText;
    
    [Header("UI References - Packets")]
    public TextMeshProUGUI packetRateText;
    public TextMeshProUGUI messageRateText;
    public TextMeshProUGUI packetTotalsText;
    
    [Header("UI References - Performance")]
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI batchingEfficiencyText;
    public TextMeshProUGUI avgMessagesPerPacketText;
    
    [Header("UI References - Summary")]
    public TextMeshProUGUI summaryText; // Combined stats for compact view
    
    [Header("Settings")]
    public bool showStats = true;
    public float updateInterval = 0.5f;
    public bool compactMode = false; // Show only summary text

    private HeartbeatManager heartbeat;
    private NetworkStatistics currentStats;
    private float lastUpdateTime;

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
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        
        // Get current statistics from active client or server
        if (gm.gameClient != null)
        {
            currentStats = gm.gameClient.Statistics;
        }
        else if (gm.gameServer != null)
        {
            currentStats = gm.gameServer.Statistics;
        }
        else
        {
            currentStats = null;
        }
        
        if (compactMode)
        {
            UpdateCompactView();
        }
        else
        {
            UpdateDetailedView();
        }
    }

    private void UpdateCompactView()
    {
        if (summaryText == null) return;
        
        GameManager gm = GameManager.Instance;
        float fps = 1f / Time.deltaTime;
        
        string summary = "";
        
        // Connection status
        if (gm.gameClient != null)
        {
            summary += "<color=#00FF00>Connected</color> | ";
        }
        else if (gm.gameServer != null)
        {
            int peerCount = gm.gameServer.GetPeerCount();
            summary += $"<color=#00FFFF>Hosting ({peerCount})</color> | ";
        }
        else
        {
            summary += "<color=#FF0000>Disconnected</color> | ";
        }
        
        // FPS
        summary += $"FPS: {fps:F0} | ";
        
        // RTT
        if (heartbeat != null)
        {
            float rtt = heartbeat.GetCurrentRtt();
            Color rttColor = heartbeat.IsConnectionHealthy() ? Color.green : Color.red;
            string rttColorHex = ColorUtility.ToHtmlStringRGB(rttColor);
            summary += $"<color=#{rttColorHex}>RTT: {rtt:F0}ms</color> | ";
        }
        
        // Bandwidth
        if (currentStats != null)
        {
            summary += $"↑{NetworkStatistics.FormatBandwidth(currentStats.CurrentUploadBandwidth)} ";
            summary += $"↓{NetworkStatistics.FormatBandwidth(currentStats.CurrentDownloadBandwidth)} | ";
            summary += $"Msgs: {currentStats.MessagesSentPerSecond + currentStats.MessagesReceivedPerSecond}/s";
        }
        
        summaryText.text = summary;
    }

    private void UpdateDetailedView()
    {
        GameManager gm = GameManager.Instance;
        
        // Connection Status
        UpdateConnectionStatus();
        
        // Latency Stats
        UpdateLatencyStats();
        
        // Bandwidth Stats
        UpdateBandwidthStats();
        
        // Packet Stats
        UpdatePacketStats();
        
        // Performance Stats
        UpdatePerformanceStats();
    }

    private void UpdateConnectionStatus()
    {
        GameManager gm = GameManager.Instance;
        
        if (connectionStatusText != null)
        {
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
        
        if (uptimeText != null && currentStats != null)
        {
            float uptime = currentStats.ConnectionUptime;
            int minutes = (int)(uptime / 60);
            int seconds = (int)(uptime % 60);
            uptimeText.text = $"Uptime: {minutes:D2}:{seconds:D2}";
        }
        
        if (playerCountText != null)
        {
            int playerCount = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length;
            playerCountText.text = $"Players: {playerCount}";
        }
    }

    private void UpdateLatencyStats()
    {
        if (rttText != null && heartbeat != null)
        {
            float rtt = heartbeat.GetCurrentRtt();
            bool isHealthy = heartbeat.IsConnectionHealthy();
            
            Color color = isHealthy ? Color.green : Color.red;
            rttText.text = $"RTT: {rtt:F1}ms";
            rttText.color = color;
        }
        
        if (jitterText != null && heartbeat != null)
        {
            float jitter = heartbeat.GetRttJitter();
            jitterText.text = $"Jitter: {jitter:F1}ms";
            
            // Color code jitter (low = green, high = red)
            if (jitter < 5f)
                jitterText.color = Color.green;
            else if (jitter < 20f)
                jitterText.color = Color.yellow;
            else
                jitterText.color = Color.red;
        }
    }

    private void UpdateBandwidthStats()
    {
        if (currentStats == null) return;
        
        if (uploadBandwidthText != null)
        {
            string upload = NetworkStatistics.FormatBandwidth(currentStats.CurrentUploadBandwidth);
            string peakUpload = NetworkStatistics.FormatBandwidth(currentStats.PeakUploadBandwidth);
            uploadBandwidthText.text = $"Upload: {upload}\n(Peak: {peakUpload})";
        }
        
        if (downloadBandwidthText != null)
        {
            string download = NetworkStatistics.FormatBandwidth(currentStats.CurrentDownloadBandwidth);
            string peakDownload = NetworkStatistics.FormatBandwidth(currentStats.PeakDownloadBandwidth);
            downloadBandwidthText.text = $"Download: {download}\n(Peak: {peakDownload})";
        }
        
        if (totalTrafficText != null)
        {
            string sent = NetworkStatistics.FormatBytes(currentStats.TotalBytesSent);
            string received = NetworkStatistics.FormatBytes(currentStats.TotalBytesReceived);
            long total = currentStats.TotalBytesSent + currentStats.TotalBytesReceived;
            string totalFormatted = NetworkStatistics.FormatBytes(total);
            totalTrafficText.text = $"Total: {totalFormatted}\n(↑{sent} ↓{received})";
        }
    }

    private void UpdatePacketStats()
    {
        if (currentStats == null) return;
        
        if (packetRateText != null)
        {
            int sentPerSec = currentStats.PacketsSentPerSecond;
            int recvPerSec = currentStats.PacketsReceivedPerSecond;
            packetRateText.text = $"Packets/s: {sentPerSec + recvPerSec}\n(↑{sentPerSec} ↓{recvPerSec})";
        }
        
        if (messageRateText != null)
        {
            int sentPerSec = currentStats.MessagesSentPerSecond;
            int recvPerSec = currentStats.MessagesReceivedPerSecond;
            messageRateText.text = $"Messages/s: {sentPerSec + recvPerSec}\n(↑{sentPerSec} ↓{recvPerSec})";
        }
        
        if (packetTotalsText != null)
        {
            packetTotalsText.text = $"Total Packets:\n↑{currentStats.TotalPacketsSent} ↓{currentStats.TotalPacketsReceived}";
        }
    }

    private void UpdatePerformanceStats()
    {
        if (fpsText != null)
        {
            float fps = 1f / Time.deltaTime;
            fpsText.text = $"FPS: {fps:F0}";
            
            // Color code FPS
            if (fps >= 60)
                fpsText.color = Color.green;
            else if (fps >= 30)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.red;
        }
        
        if (currentStats == null) return;
        
        if (batchingEfficiencyText != null)
        {
            float efficiency = currentStats.BatchingEfficiency;
            batchingEfficiencyText.text = $"Batching: {efficiency:F1}%";
            
            // Color code efficiency
            if (efficiency >= 70f)
                batchingEfficiencyText.color = Color.green;
            else if (efficiency >= 40f)
                batchingEfficiencyText.color = Color.yellow;
            else
                batchingEfficiencyText.color = Color.red;
        }
        
        if (avgMessagesPerPacketText != null)
        {
            float avgMsgs = currentStats.AverageMessagesPerPacket;
            avgMessagesPerPacketText.text = $"Avg Msgs/Packet: {avgMsgs:F2}";
        }
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
    /// Toggle between compact and detailed view
    /// </summary>
    public void ToggleCompactMode()
    {
        compactMode = !compactMode;
    }
}
