using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class GameManager : MonoBehaviour
{
    public class PlayerEntry
    {
        public int playerId;
        public int connectionId;
        public GameObject playerObject;
    }

    // Mapeos simples
    private Dictionary<int, PlayerEntry> connectionToPlayer = new Dictionary<int, PlayerEntry>();
    private Dictionary<int, PlayerEntry> idToPlayer = new Dictionary<int, PlayerEntry>();

    private int nextPlayerId = 1;
    
    public UnityEvent<int> OnPlayerRegistered;
    public UnityEvent<int> OnPlayerUnregistered;

    void Awake()
    {
        if (OnPlayerRegistered == null) OnPlayerRegistered = new UnityEvent<int>();
        if (OnPlayerUnregistered == null) OnPlayerUnregistered = new UnityEvent<int>();
    } 
    
    //register player in gameManager
    public int RegisterPlayer(int connectionId, GameObject playerObject)
    {
        if (connectionToPlayer.ContainsKey(connectionId))
        {
            Debug.LogWarning($"GameManager: connection {connectionId} ya registrada");
            return connectionToPlayer[connectionId].playerId;
        }

        int pid = nextPlayerId++;
        var entry = new PlayerEntry
        {
            playerId = pid,
            connectionId = connectionId,
            playerObject = playerObject
        };

        connectionToPlayer[connectionId] = entry;
        idToPlayer[pid] = entry;

        OnPlayerRegistered.Invoke(pid);
        Debug.Log($"GameManager: Registered playerId={pid} for connection={connectionId}");
        return pid;
    }
        
    public void UnregisterPlayerByConnection(int connectionId)
    {
        if (!connectionToPlayer.TryGetValue(connectionId, out var entry)) return;
        UnregisterPlayer(entry.playerId);
    }
        
    public void UnregisterPlayer(int playerId)
    {
        if (!idToPlayer.TryGetValue(playerId, out var entry)) return;

        connectionToPlayer.Remove(entry.connectionId);
        idToPlayer.Remove(playerId);

        OnPlayerUnregistered.Invoke(playerId);
        Debug.Log($"GameManager: Unregistered playerId={playerId} connection={entry.connectionId}");
    }
        
    //para q podamos acceder al player y evitar errores, esta bn? 
    public bool TryGetPlayerByConnection(int connectionId, out PlayerEntry player)
    {
        return connectionToPlayer.TryGetValue(connectionId, out player);
    }
        
    public bool TryGetPlayerById(int playerId, out PlayerEntry player)
    {
        return idToPlayer.TryGetValue(playerId, out player);
    }
        
    public int[] GetAllPlayerIds()
    {
        var keys = new int[idToPlayer.Count];
        idToPlayer.Keys.CopyTo(keys, 0);
        return keys;
    }
}
