using UnityEngine;

// A simple class to hold data about a connected peer.

public class Peer
{
    public int ConnectionId { get; private set; }
    
    // Player identity (game-level)
    public int PlayerId { get; set; } = -1;
    public string Username { get; set; } = "";
    public GameObject PlayerObject { get; set; } = null;
    
    // Heartbeat tracking
    public float LastPingTime { get; set; }
    public float LastPongTime { get; set; }
    
    // Connection state
    public bool IsPlayerSpawned => PlayerObject != null;

    public Peer(int connectionId)
    {
        ConnectionId = connectionId;
        LastPingTime = Time.time;
        LastPongTime = Time.time;
    }

    /// <summary>
    /// Returns time since last pong in seconds
    /// </summary>
    public float GetTimeSinceLastPong()
    {
        return Time.time - LastPongTime;
    }
    
    /// <summary>
    /// Get display name with fallback
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(Username) ? $"Player {PlayerId}" : Username;
    }
}
