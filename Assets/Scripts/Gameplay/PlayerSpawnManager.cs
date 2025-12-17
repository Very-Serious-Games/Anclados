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

    private float lastStateBroadcast = 0f;
    private float stateBroadcastRate = 0.05f; // 20Hz
    private NetworkUpdateManager networkUpdateManager;

    void Start()
    {
        gameServer = GameManager.Instance.gameServer;
        gameClient = GameManager.Instance.gameClient;

        // Get NetworkUpdateManager reference
        networkUpdateManager = FindFirstObjectByType<NetworkUpdateManager>();
        
        if (networkUpdateManager != null && gameServer != null)
        {
            // Subscribe to network tick for state broadcasts
            networkUpdateManager.OnNetworkTick += OnNetworkTick;
        }

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
            
            // If we're the host, spawn our own local player immediately FIRST
            if (GameManager.Instance.connectionType == ConnectionType.Host)
            {
                // Host assigns itself player ID 1 (first player)
                localPlayerId = nextPlayerId++;
                
                // Find the host's peer (should be the first one connected to itself)
                Peer hostPeer = null;
                foreach (var peer in gameServer.GetConnectedPeers().Values)
                {
                    if (peer.PlayerId == -1) // Not yet assigned
                    {
                        peer.PlayerId = localPlayerId;
                        hostPeer = peer;
                        gameServer.InitializeAckTracking(peer.PlayerId);
                        Debug.Log($"[PlayerSpawnManager - Host] Assigned PlayerId {localPlayerId} to host peer (ConnectionId: {peer.ConnectionId})");
                        break;
                    }
                }
                
                // Spawn local player for host
                Vector3 spawnPos = GetNextSpawnPosition();
                Quaternion spawnRot = Quaternion.identity;
                GameObject playerObj = SpawnPlayerLocal(localPlayerId, GameManager.Instance.GetUsername(), spawnPos, spawnRot, true);
                
                if (hostPeer != null)
                {
                    hostPeer.PlayerObject = playerObj;
                    hostPeer.Username = GameManager.Instance.GetUsername();
                }
                
                Debug.Log($"[PlayerSpawnManager - Host] Spawned local player {localPlayerId} at {spawnPos}");
                
                // Now handle already-connected peers (connected during lobby) - they get IDs 2, 3, etc.
                foreach (var peer in gameServer.GetConnectedPeers().Values)
                {
                    if (peer.PlayerId == -1) // Not yet assigned (skip host peer which was just assigned)
                    {
                        HandleServerPlayerConnected(peer);
                    }
                }
            }
            else
            {
                // Regular clients send join message
                gameClient.SendJoinMessage();
            }
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
        
        if (networkUpdateManager != null)
        {
            networkUpdateManager.OnNetworkTick -= OnNetworkTick;
        }
    }

    // -------- SERVER LOGIC -------- //

    private void HandleServerPlayerConnected(Peer peer)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Player connected: {peer.ConnectionId}");
        
        // Only assign PlayerId if not already assigned
        if (peer.PlayerId == -1)
        {
            peer.PlayerId = nextPlayerId++;
            Debug.Log($"[PlayerSpawnManager - Server] Assigned PlayerId {peer.PlayerId} to connection {peer.ConnectionId}");
        }
        else
        {
            Debug.Log($"[PlayerSpawnManager - Server] Connection {peer.ConnectionId} already has PlayerId {peer.PlayerId}");
        }
        
        // Initialize ACK tracking for this player using NetworkServer's ACK system
        gameServer.InitializeAckTracking(peer.PlayerId);
    }
    
    private void SpawnPlayerForPeer(Peer peer, string username)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Spawning player for connection {peer.ConnectionId}");
        
        peer.Username = username;

        // Get spawn position
        Vector3 spawnPos = GetNextSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        // IMPORTANT: Send player ID assignment FIRST
        AssignPlayerIdMessage assignMsg = new AssignPlayerIdMessage(peer.PlayerId);
        gameServer.Send(peer, assignMsg);

        // Spawn player on server (always as remote from server's perspective)
        // Note: Host already spawned itself as local, so this creates remote representation
        GameObject playerObj = SpawnPlayerLocal(peer.PlayerId, peer.Username, spawnPos, spawnRot, false);
        peer.PlayerObject = playerObj;

        // Tell this client about existing players (reliable)
        foreach (var existingPeer in gameServer.GetConnectedPeers().Values)
        {
            if (existingPeer.ConnectionId == peer.ConnectionId) continue;
            if (existingPeer.PlayerId == -1) continue;
            if (!existingPeer.IsPlayerSpawned) continue;

            SpawnPlayerMessage existingMsg = new SpawnPlayerMessage(
                existingPeer.PlayerId,
                existingPeer.Username,
                existingPeer.PlayerObject.transform.position,
                existingPeer.PlayerObject.transform.rotation
            );
            gameServer.Send(peer, existingMsg);
        }

        // Tell the new player to spawn themselves (after they know existing players)
        SpawnPlayerMessage selfSpawnMsg = new SpawnPlayerMessage(peer.PlayerId, peer.Username, spawnPos, spawnRot);
        gameServer.Send(peer, selfSpawnMsg);

        // Broadcast new player to all other clients (excluding the new player themselves)
        SpawnPlayerMessage broadcastMsg = new SpawnPlayerMessage(peer.PlayerId, peer.Username, spawnPos, spawnRot);
        gameServer.Broadcast(broadcastMsg, peer);
        
        Debug.Log($"[PlayerSpawnManager - Server] Spawned player {peer.PlayerId} at {spawnPos}");
    }

    private void HandleServerPlayerDisconnected(Peer peer)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Player disconnected: {peer.ConnectionId}, PlayerId: {peer.PlayerId}");

        if (peer.PlayerId != -1)
        {
            // ACK tracking cleanup is handled by NetworkServer
            
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
        else if (message is StateAckMessage ackMsg)
        {
            ProcessStateAck(peer, ackMsg);
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

        Debug.Log($"[PlayerSpawnManager - SERVER] Processing input for player {peer.PlayerId} - Forward:{input.forward} Backward:{input.backward}");
        
        // Server applies input to controller for physics processing
        controller.ApplyInputFromServer(input);
        
        // IMMEDIATELY send authoritative state update after processing input
        SendStateUpdate(peer, controller, input.sequenceNumber, true);
    }

    private void SendStateUpdate(Peer peer, NetworkPlayerController controller, int inputSequence, bool isImmediate)
    {
        // Get current state
        PlayerStateMessage stateMsg = controller.GetCurrentState(inputSequence);
        
        // Get sequence number from NetworkServer's ACK system and track the message
        int stateSeq = gameServer.GetNextSequence(peer.PlayerId, stateMsg);
        stateMsg.stateSequence = stateSeq;
        
        // Send state update
        gameServer.Send(peer, stateMsg);
        
        // Log with unacked count
        int unackedCount = gameServer.GetUnackedCount(peer.PlayerId);
        if (isImmediate)
        {
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] Sent IMMEDIATE state {stateSeq} to player {peer.PlayerId} after input (Unacked: {unackedCount})");
        }
        else
        {
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] Sent PERIODIC state {stateSeq} to player {peer.PlayerId} (Unacked: {unackedCount})");
        }
    }

    private void ProcessStateAck(Peer peer, StateAckMessage ackMsg)
    {
        // Delegate to NetworkServer's ACK system (it handles all logging)
        gameServer.ProcessAck(ackMsg.playerId, ackMsg.stateSequence);
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
            
            // If player was already spawned (from SpawnPlayerMessage), update the local flag
            if (spawnedPlayers.TryGetValue(localPlayerId, out GameObject playerObj))
            {
                NetworkPlayer netPlayer = playerObj.GetComponent<NetworkPlayer>();
                NetworkPlayerController controller = playerObj.GetComponent<NetworkPlayerController>();
                NetworkHealth health = playerObj.GetComponent<NetworkHealth>();
                
                if (netPlayer != null)
                    netPlayer.isLocalPlayer = true;
                
                if (controller != null)
                    controller.isLocalPlayer = true;
                
                if (health != null)
                    health.isLocalPlayer = true;
                
                // Enable camera for this player
                Camera playerCamera = playerObj.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    playerCamera.enabled = true;
                    playerCamera.gameObject.SetActive(true);
                    
                    AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
                    if (audioListener != null)
                    {
                        audioListener.enabled = true;
                    }
                    
                    Debug.Log($"[PlayerSpawnManager - Client] Enabled camera for assigned local player {localPlayerId}");
                }
                
                Debug.Log($"[PlayerSpawnManager - Client] Updated player {localPlayerId} to local player");
            }
            else
            {
                // Player not spawned yet, request spawn position from server
                // For now, we'll wait for the SpawnPlayerMessage
                // In a production system, the server should send position with AssignPlayerIdMessage
                Debug.Log($"[PlayerSpawnManager - Client] Waiting for spawn position for local player {localPlayerId}");
            }
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
            bool isLocal = (stateMsg.playerId == localPlayerId);
            Debug.Log($"[PlayerSpawnManager - CLIENT - ACK] ← Received state {stateMsg.stateSequence} for player {stateMsg.playerId} (IsLocal:{isLocal})");
            
            ApplyPlayerState(stateMsg);
            
            // Send ACK back to server for this state
            StateAckMessage ackMsg = new StateAckMessage(stateMsg.playerId, stateMsg.stateSequence);
            gameClient.Send(ackMsg);
            
            Debug.Log($"[PlayerSpawnManager - CLIENT - ACK] → Sent ACK for state {stateMsg.stateSequence} of player {stateMsg.playerId}");
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
            if (controller != null)
            {
                bool isLocal = (state.playerId == localPlayerId);
                Debug.Log($"[PlayerSpawnManager - CLIENT - ACK] ✓ Applied state {state.stateSequence} for player {state.playerId} (IsLocal:{isLocal}) Pos:{state.position}");
                controller.ApplyNetworkState(state);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager - CLIENT - ACK] ✗ Cannot apply state {state.stateSequence} - player {state.playerId} not found");
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

        // Enable camera only for local player
        Camera playerCamera = playerObj.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.enabled = isLocal;
            playerCamera.gameObject.SetActive(isLocal);
            
            if (isLocal)
            {
                // Ensure AudioListener is also enabled for local player
                AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = true;
                }
                
                Debug.Log($"[PlayerSpawnManager] Enabled camera for local player {playerId}");
            }
            else
            {
                Debug.Log($"[PlayerSpawnManager] Camera disabled for remote player {playerId}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] No camera found in player {playerId} hierarchy!");
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

    /// <summary>
    /// Called at network tick rate (e.g., 20Hz)
    /// </summary>
    private void OnNetworkTick()
    {
        // Server broadcasts player states at network tick rate
        if (gameServer != null && GameManager.Instance.connectionType == ConnectionType.Host)
        {
            BroadcastPlayerStates();
        }
    }

    private void BroadcastPlayerStates()
    {
        // Remove time check since we're already at correct rate
        foreach (var kvp in spawnedPlayers)
        {
            int playerId = kvp.Key;
            NetworkPlayerController controller = kvp.Value.GetComponent<NetworkPlayerController>();
            if (controller != null)
            {
                // Find the peer for this player
                Peer targetPeer = null;
                foreach (var peer in gameServer.GetConnectedPeers().Values)
                {
                    if (peer.PlayerId == playerId)
                    {
                        targetPeer = peer;
                        break;
                    }
                }
                
                if (targetPeer != null)
                {
                    // Send periodic update
                    SendStateUpdate(targetPeer, controller, 0, false);
                }
                else
                {
                    Debug.LogWarning($"[PlayerSpawnManager - SERVER - ACK] No peer found for player {playerId} during periodic broadcast");
                }
            }
        }
    }
}
