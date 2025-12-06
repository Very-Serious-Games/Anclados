using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawning and despawning of network players.
/// Handles player assignment, instantiation, and cleanup.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    public float spawnRadius = 5f;

    [Header("State")]
    private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
    private int nextPlayerId = 1;
    private int nextSpawnIndex = 0;

    private NetworkServer gameServer;
    private NetworkClient gameClient;

    void Start()
    {
        gameServer = GameManager.Instance.gameServer;
        gameClient = GameManager.Instance.gameClient;

        // Subscribe to server events (if we're hosting)
        if (gameServer != null)
        {
            gameServer.OnPlayerConnected += HandleServerPlayerConnected;
            gameServer.OnPlayerDisconnected += HandleServerPlayerDisconnected;
            gameServer.OnMessageReceived += HandleServerMessage;
        }

        // Subscribe to client events
        if (gameClient != null)
        {
            gameClient.OnMessageReceived += HandleClientMessage;
        }
    }

    void OnDestroy()
    {
        if (gameServer != null)
        {
            gameServer.OnPlayerConnected -= HandleServerPlayerConnected;
            gameServer.OnPlayerDisconnected -= HandleServerPlayerDisconnected;
            gameServer.OnMessageReceived -= HandleServerMessage;
        }

        if (gameClient != null)
        {
            gameClient.OnMessageReceived -= HandleClientMessage;
        }
    }

    // -------- SERVER LOGIC -------- //

    private void HandleServerPlayerConnected(Peer peer)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Player connected: {peer.ConnectionId}");
        
        // Don't spawn yet - wait for JoinMessage with username
        // Just assign the PlayerId for now
        peer.PlayerId = nextPlayerId++;
    }
    
    private void SpawnPlayerForPeer(Peer peer, string username)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Spawning player for connection {peer.ConnectionId}");
        
        peer.Username = username;

        // Get spawn position
        Vector3 spawnPos = GetNextSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        // Spawn player on server
        GameObject playerObj = SpawnPlayerLocal(peer.PlayerId, peer.Username, spawnPos, spawnRot, false);
        peer.PlayerObject = playerObj;

        // Send player ID assignment to this client
        AssignPlayerIdMessage assignMsg = new AssignPlayerIdMessage(peer.PlayerId);
        gameServer.Send(peer, assignMsg);

        // Tell this client about existing players
        foreach (var existingPeer in gameServer.GetConnectedPeers().Values)
        {
            if (existingPeer.ConnectionId == peer.ConnectionId) continue;
            if (existingPeer.PlayerId == -1) continue;
            if (!existingPeer.IsPlayerSpawned) continue; // Only send already spawned players

            SpawnPlayerMessage existingMsg = new SpawnPlayerMessage(
                existingPeer.PlayerId,
                existingPeer.Username,
                existingPeer.PlayerObject.transform.position,
                existingPeer.PlayerObject.transform.rotation
            );
            gameServer.Send(peer, existingMsg);
        }

        // Broadcast new player to all other clients
        SpawnPlayerMessage spawnMsg = new SpawnPlayerMessage(peer.PlayerId, peer.Username, spawnPos, spawnRot);
        gameServer.Broadcast(spawnMsg, peer);
        
        Debug.Log($"[PlayerSpawnManager - Server] Spawned player {peer.PlayerId} at {spawnPos}");
    }

    private void HandleServerPlayerDisconnected(Peer peer)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Player disconnected: {peer.ConnectionId}, PlayerId: {peer.PlayerId}");

        if (peer.PlayerId != -1)
        {
            // Despawn player locally
            DespawnPlayerLocal(peer.PlayerId);

            // Broadcast despawn to all clients
            DespawnPlayerMessage despawnMsg = new DespawnPlayerMessage(peer.PlayerId);
            gameServer.Broadcast(despawnMsg);
        }
    }

    private void HandleServerMessage(Peer peer, INetworkMessage message)
    {
        // Handle join message - spawn player when client announces itself
        if (message is JoinMessage joinMsg)
        {
            SpawnPlayerForPeer(peer, joinMsg.username);
        }
        // Server processes player inputs here
        else if (message is PlayerInputMessage inputMsg)
        {
            ProcessPlayerInput(peer, inputMsg);
        }
        else if (message is FireCannonMessage fireMsg)
        {
            ProcessFireCannon(peer, fireMsg);
        }
    }

    private void ProcessPlayerInput(Peer peer, PlayerInputMessage input)
    {
        // Find player object
        if (!spawnedPlayers.TryGetValue(peer.PlayerId, out GameObject playerObj))
        {
            Debug.LogWarning($"[PlayerSpawnManager] No player object for PlayerId {peer.PlayerId}");
            return;
        }

        NetworkPlayerController controller = playerObj.GetComponent<NetworkPlayerController>();
        if (controller == null)
        {
            Debug.LogWarning($"[PlayerSpawnManager] No NetworkPlayerController on player {peer.PlayerId}");
            return;
        }

        Debug.Log($"[PlayerSpawnManager] Processing input for player {peer.PlayerId} - Forward:{input.forward} Backward:{input.backward}");
        // Server applies input to controller for physics processing
        controller.ApplyInputFromServer(input);
    }

    private void ProcessFireCannon(Peer peer, FireCannonMessage fireMsg)
    {
        // Server validates and spawns cannonball
        Debug.Log($"[PlayerSpawnManager - Server] Player {fireMsg.playerId} fired cannon");

        // TODO: Spawn cannonball and broadcast to all clients
        SpawnCannonballMessage cannonballMsg = new SpawnCannonballMessage(
            Random.Range(1000, 9999), // Cannonball ID
            fireMsg.playerId,
            fireMsg.position,
            fireMsg.direction * 40f, // Velocity
            5f, // Lifetime
            Time.time
        );

        gameServer.Broadcast(cannonballMsg);
    }

    // -------- CLIENT LOGIC -------- //

    private int localPlayerId = -1;

    private void HandleClientMessage(INetworkMessage message)
    {
        if (message is AssignPlayerIdMessage assignMsg)
        {
            localPlayerId = assignMsg.assignedPlayerId;
            Debug.Log($"[PlayerSpawnManager - Client] Assigned player ID: {localPlayerId}");
        }
        else if (message is SpawnPlayerMessage spawnMsg)
        {
            bool isLocal = (spawnMsg.playerId == localPlayerId);
            SpawnPlayerLocal(spawnMsg.playerId, spawnMsg.username, spawnMsg.spawnPosition, spawnMsg.spawnRotation, isLocal);
        }
        else if (message is DespawnPlayerMessage despawnMsg)
        {
            DespawnPlayerLocal(despawnMsg.playerId);
        }
        else if (message is PlayerStateMessage stateMsg)
        {
            ApplyPlayerState(stateMsg);
        }
        else if (message is SpawnCannonballMessage cannonballMsg)
        {
            SpawnCannonball(cannonballMsg);
        }
    }

    private void ApplyPlayerState(PlayerStateMessage state)
    {
        if (spawnedPlayers.TryGetValue(state.playerId, out GameObject playerObj))
        {
            NetworkPlayerController controller = playerObj.GetComponent<NetworkPlayerController>();
            controller?.ApplyNetworkState(state);
        }
    }

    // -------- SHARED LOGIC -------- //

    private GameObject SpawnPlayerLocal(int playerId, string username, Vector3 position, Quaternion rotation, bool isLocal)
    {
        if (spawnedPlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"[PlayerSpawnManager] Player {playerId} already spawned!");
            return spawnedPlayers[playerId];
        }

        GameObject playerObj = Instantiate(playerPrefab, position, rotation);
        spawnedPlayers[playerId] = playerObj;

        // Initialize network player component
        NetworkPlayer netPlayer = playerObj.GetComponent<NetworkPlayer>();
        if (netPlayer != null)
        {
            netPlayer.Initialize(playerId, -1, username, isLocal);
        }

        // Initialize controller
        NetworkPlayerController controller = playerObj.GetComponent<NetworkPlayerController>();
        if (controller != null)
        {
            controller.isLocalPlayer = isLocal;
            controller.playerId = playerId;
        }

        Debug.Log($"[PlayerSpawnManager] Spawned player {playerId} ({username}) - IsLocal: {isLocal}");
        return playerObj;
    }

    private void DespawnPlayerLocal(int playerId)
    {
        if (spawnedPlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            spawnedPlayers.Remove(playerId);
            Destroy(playerObj);
            Debug.Log($"[PlayerSpawnManager] Despawned player {playerId}");
        }
    }

    private Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Vector3 pos = spawnPoints[nextSpawnIndex].position;
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
            return pos;
        }

        // Random spawn around origin
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    private void SpawnCannonball(SpawnCannonballMessage msg)
    {
        // TODO: Instantiate cannonball prefab
        Debug.Log($"[PlayerSpawnManager] Spawning cannonball {msg.cannonballId} from player {msg.ownerId}");
        
        // For now, just log - full implementation needs CannonBallManager integration
    }

    void Update()
    {
        // Server broadcasts player states periodically
        if (gameServer != null && GameManager.Instance.connectionType == ConnectionType.Host)
        {
            BroadcastPlayerStates();
        }
    }

    private float lastStateBroadcast = 0f;
    private float stateBroadcastRate = 0.05f; // 20Hz

    private void BroadcastPlayerStates()
    {
        if (Time.time - lastStateBroadcast < stateBroadcastRate)
            return;

        lastStateBroadcast = Time.time;

        foreach (var kvp in spawnedPlayers)
        {
            NetworkPlayerController controller = kvp.Value.GetComponent<NetworkPlayerController>();
            if (controller != null)
            {
                PlayerStateMessage stateMsg = controller.GetCurrentState(0);
                gameServer.Broadcast(stateMsg);
            }
        }
    }
}
