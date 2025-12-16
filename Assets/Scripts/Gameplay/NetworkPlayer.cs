using UnityEngine;

/// <summary>
/// Identifies a player in the network. Distinguishes local vs remote players.
/// Attach to player prefab instances.
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    [Header("Network Identity")]
    public int playerId = -1;              // Unique game-level player ID
    public bool isLocalPlayer = false;     // True if this is controlled by local client
    public string username = "";           // Player display name
    
    [Header("Connection Info")]
    public int connectionId = -1;          // Transport-level connection ID
    public float spawnTime;

    // Components
    private NetworkPlayerController controller;
    private NetworkHealth health;
    
    void Awake()
    {
        controller = GetComponent<NetworkPlayerController>();
        health = GetComponent<NetworkHealth>();
        spawnTime = Time.time;
    }

    public void Initialize(int playerId, int connectionId, string username, bool isLocal)
    {
        this.playerId = playerId;
        this.connectionId = connectionId;
        this.username = username;
        this.isLocalPlayer = isLocal;
        
        Debug.Log($"[NetworkPlayer] Initialized - ID: {playerId}, Username: {username}, IsLocal: {isLocal}");
        
        // Configure components based on local/remote
        if (controller != null)
        {
            controller.isLocalPlayer = isLocal;
        }
        
        if (health != null)
        {
            health.isLocalPlayer = isLocal;
        }
    }

    /// <summary>
    /// Get the display name with fallback
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(username) ? $"Player {playerId}" : username;
    }

    void OnDestroy()
    {
        Debug.Log($"[NetworkPlayer] Destroyed - ID: {playerId}, Username: {username}");
    }
}
