using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks comprehensive network statistics for monitoring and debugging
/// </summary>
public class NetworkStatistics
{
    // Traffic statistics
    private long _totalBytesSent = 0;
    private long _totalBytesReceived = 0;
    private long _bytesSentThisSecond = 0;
    private long _bytesReceivedThisSecond = 0;
    private float _lastBandwidthUpdate = 0f;
    
    // Packet statistics
    private int _totalPacketsSent = 0;
    private int _totalPacketsReceived = 0;
    private int _packetsSentThisSecond = 0;
    private int _packetsReceivedThisSecond = 0;
    
    // Message statistics (for batching)
    private int _totalMessagesSent = 0;
    private int _totalMessagesReceived = 0;
    private int _messagesSentThisSecond = 0;
    private int _messagesReceivedThisSecond = 0;
    
    // Bandwidth tracking
    private float _currentUploadBandwidth = 0f; // bytes per second
    private float _currentDownloadBandwidth = 0f; // bytes per second
    private float _peakUploadBandwidth = 0f;
    private float _peakDownloadBandwidth = 0f;
    
    // RTT tracking for jitter calculation
    private Queue<float> _recentRttValues = new Queue<float>();
    private const int MaxRttSamples = 20;
    private float _averageRtt = 0f;
    private float _rttJitter = 0f;
    
    // Connection timing
    private float _connectionStartTime = 0f;
    
    // Packet loss (requires acknowledgment system)
    private int _packetsLost = 0;
    
    // Batching statistics
    private int _batchedPackets = 0;
    private int _unbatchedPackets = 0;

    public NetworkStatistics()
    {
        Reset();
    }

    /// <summary>
    /// Reset all statistics
    /// </summary>
    public void Reset()
    {
        _totalBytesSent = 0;
        _totalBytesReceived = 0;
        _totalPacketsSent = 0;
        _totalPacketsReceived = 0;
        _totalMessagesSent = 0;
        _totalMessagesReceived = 0;
        _packetsLost = 0;
        _batchedPackets = 0;
        _unbatchedPackets = 0;
        _recentRttValues.Clear();
        _connectionStartTime = Time.time;
        _peakUploadBandwidth = 0f;
        _peakDownloadBandwidth = 0f;
    }

    /// <summary>
    /// Update bandwidth calculations (call this every frame or at regular intervals)
    /// </summary>
    public void Update()
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - _lastBandwidthUpdate;
        
        if (deltaTime >= 1f)
        {
            // Calculate bandwidth in bytes per second
            _currentUploadBandwidth = _bytesSentThisSecond / deltaTime;
            _currentDownloadBandwidth = _bytesReceivedThisSecond / deltaTime;
            
            // Update peaks
            if (_currentUploadBandwidth > _peakUploadBandwidth)
                _peakUploadBandwidth = _currentUploadBandwidth;
            if (_currentDownloadBandwidth > _peakDownloadBandwidth)
                _peakDownloadBandwidth = _currentDownloadBandwidth;
            
            // Reset counters
            _bytesSentThisSecond = 0;
            _bytesReceivedThisSecond = 0;
            _packetsSentThisSecond = 0;
            _packetsReceivedThisSecond = 0;
            _messagesSentThisSecond = 0;
            _messagesReceivedThisSecond = 0;
            _lastBandwidthUpdate = currentTime;
        }
    }

    // Recording methods
    public void RecordPacketSent(int byteCount, int messageCount = 1, bool wasBatched = false)
    {
        _totalPacketsSent++;
        _packetsSentThisSecond++;
        _totalBytesSent += byteCount;
        _bytesSentThisSecond += byteCount;
        _totalMessagesSent += messageCount;
        _messagesSentThisSecond += messageCount;
        
        if (wasBatched && messageCount > 1)
            _batchedPackets++;
        else
            _unbatchedPackets++;
    }

    public void RecordPacketReceived(int byteCount, int messageCount = 1)
    {
        _totalPacketsReceived++;
        _packetsReceivedThisSecond++;
        _totalBytesReceived += byteCount;
        _bytesReceivedThisSecond += byteCount;
        _totalMessagesReceived += messageCount;
        _messagesReceivedThisSecond += messageCount;
    }

    public void RecordRtt(float rttMilliseconds)
    {
        _recentRttValues.Enqueue(rttMilliseconds);
        if (_recentRttValues.Count > MaxRttSamples)
            _recentRttValues.Dequeue();
        
        // Calculate average and jitter
        if (_recentRttValues.Count > 0)
        {
            _averageRtt = _recentRttValues.Average();
            
            // Jitter = average deviation from mean
            float sumDeviation = 0f;
            foreach (float rtt in _recentRttValues)
            {
                sumDeviation += Mathf.Abs(rtt - _averageRtt);
            }
            _rttJitter = sumDeviation / _recentRttValues.Count;
        }
    }

    public void RecordPacketLost()
    {
        _packetsLost++;
    }

    // Getters
    public long TotalBytesSent => _totalBytesSent;
    public long TotalBytesReceived => _totalBytesReceived;
    public int TotalPacketsSent => _totalPacketsSent;
    public int TotalPacketsReceived => _totalPacketsReceived;
    public int TotalMessagesSent => _totalMessagesSent;
    public int TotalMessagesReceived => _totalMessagesReceived;
    public int PacketsLost => _packetsLost;
    
    public float CurrentUploadBandwidth => _currentUploadBandwidth;
    public float CurrentDownloadBandwidth => _currentDownloadBandwidth;
    public float PeakUploadBandwidth => _peakUploadBandwidth;
    public float PeakDownloadBandwidth => _peakDownloadBandwidth;
    
    public int PacketsSentPerSecond => _packetsSentThisSecond;
    public int PacketsReceivedPerSecond => _packetsReceivedThisSecond;
    public int MessagesSentPerSecond => _messagesSentThisSecond;
    public int MessagesReceivedPerSecond => _messagesReceivedThisSecond;
    
    public float AverageRtt => _averageRtt;
    public float RttJitter => _rttJitter;
    
    public float ConnectionUptime => Time.time - _connectionStartTime;
    
    public float PacketLossRate
    {
        get
        {
            int totalPackets = _totalPacketsSent + _totalPacketsReceived;
            return totalPackets > 0 ? (_packetsLost / (float)totalPackets) * 100f : 0f;
        }
    }
    
    public float BatchingEfficiency
    {
        get
        {
            int totalPackets = _batchedPackets + _unbatchedPackets;
            return totalPackets > 0 ? (_batchedPackets / (float)totalPackets) * 100f : 0f;
        }
    }
    
    public float AverageMessagesPerPacket
    {
        get
        {
            return _totalPacketsSent > 0 ? (float)_totalMessagesSent / _totalPacketsSent : 0f;
        }
    }

    // Utility methods for formatting
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
    
    public static string FormatBandwidth(float bytesPerSecond)
    {
        return FormatBytes((long)bytesPerSecond) + "/s";
    }
}
