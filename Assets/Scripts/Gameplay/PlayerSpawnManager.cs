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

    // ACK System - Track state sequence numbers
    private Dictionary<int, int> lastAckedStateSequence = new Dictionary<int, int>();
    private Dictionary<int, int> currentStateSequence = new Dictionary<int, int>();
    
    // Track unacknowledged states per player
    private Dictionary<int, Queue<PlayerStateMessage>> unackedStates = new Dictionary<int, Queue<PlayerStateMessage>>();
    private const int MAX_UNACKED_STATES = 10; // Limit memory usage

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
            
            // Handle already-connected peers (connected during lobby)
            foreach (var peer in gameServer.GetConnectedPeers().Values)
            {
                HandleServerPlayerConnected(peer);
            }
        }

        // Subscribe to client events
        if (gameClient != null)
        {
            gameClient.OnMessageReceived += HandleClientMessage;
            
            // Send join message now that we're in the game scene
            gameClient.SendJoinMessage();
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
        
        // Initialize ACK tracking for this player
        if (!lastAckedStateSequence.ContainsKey(peer.PlayerId))
        {
            lastAckedStateSequence[peer.PlayerId] = 0;
            currentStateSequence[peer.PlayerId] = 0;
            unackedStates[peer.PlayerId] = new Queue<PlayerStateMessage>();
        }
    }
    
    private void SpawnPlayerForPeer(Peer peer, string username)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Spawning player for connection {peer.ConnectionId}");
        
        peer.Username = username;

        // Get spawn position
        Vector3 spawnPos = GetNextSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        // IMPORTANT: Send player ID assignment FIRST with reliable delivery
        AssignPlayerIdMessage assignMsg = new AssignPlayerIdMessage(peer.PlayerId);
        gameServer.SendReliable(peer, assignMsg);

        // Spawn player on server
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
            gameServer.SendReliable(peer, existingMsg);
        }

        // Broadcast new player to all other clients (reliable)
        SpawnPlayerMessage spawnMsg = new SpawnPlayerMessage(peer.PlayerId, peer.Username, spawnPos, spawnRot);
        gameServer.BroadcastReliable(spawnMsg, peer);
        
        // Also send the spawn message to the new player so they spawn themselves
        gameServer.SendReliable(peer, spawnMsg);
        
        Debug.Log($"[PlayerSpawnManager - Server] Spawned player {peer.PlayerId} at {spawnPos}");
    }

    private void HandleServerPlayerDisconnected(Peer peer)
    {
        Debug.Log($"[PlayerSpawnManager - Server] Player disconnected: {peer.ConnectionId}, PlayerId: {peer.PlayerId}");

        if (peer.PlayerId != -1)
        {
            // Cleanup ACK tracking
            lastAckedStateSequence.Remove(peer.PlayerId);
            currentStateSequence.Remove(peer.PlayerId);
            unackedStates.Remove(peer.PlayerId);
            
            // Despawn player locally
            DespawnPlayerLocal(peer.PlayerId);

            // Broadcast despawn to all clients (reliable)
            DespawnPlayerMessage despawnMsg = new DespawnPlayerMessage(peer.PlayerId);
            gameServer.BroadcastReliable(despawnMsg);
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
        if (!currentStateSequence.ContainsKey(peer.PlayerId))
        {
            currentStateSequence[peer.PlayerId] = 0;
        }
        
        // Increment state sequence
        currentStateSequence[peer.PlayerId]++;
        int stateSeq = currentStateSequence[peer.PlayerId];
        
        // Get current state
        PlayerStateMessage stateMsg = controller.GetCurrentState(inputSequence);
        stateMsg.stateSequence = stateSeq;
        
        // Track unacked state
        if (unackedStates.ContainsKey(peer.PlayerId))
        {
            // Limit queue size to prevent memory leak
            if (unackedStates[peer.PlayerId].Count >= MAX_UNACKED_STATES)
            {
                unackedStates[peer.PlayerId].Dequeue();
                Debug.LogWarning($"[PlayerSpawnManager - SERVER] Too many unacked states for player {peer.PlayerId}, dropping oldest");
            }
            
            unackedStates[peer.PlayerId].Enqueue(stateMsg);
        }
        
        // Send with reliable delivery for immediate/important updates
        if (isImmediate)
        {
            gameServer.SendReliable(peer, stateMsg);
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] Sent RELIABLE state {stateSeq} to player {peer.PlayerId} after input (Unacked: {unackedStates[peer.PlayerId].Count})");
        }
        else
        {
            // Periodic updates can be unreliable for bandwidth efficiency
            gameServer.Send(peer, stateMsg);
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] Sent UNRELIABLE state {stateSeq} to player {peer.PlayerId} (Unacked: {unackedStates[peer.PlayerId].Count})");
        }
    }

    private void ProcessStateAck(Peer peer, StateAckMessage ackMsg)
    {
        if (!lastAckedStateSequence.ContainsKey(ackMsg.playerId))
        {
            lastAckedStateSequence[ackMsg.playerId] = 0;
        }
        
        // Update last acknowledged sequence
        int previousAck = lastAckedStateSequence[ackMsg.playerId];
        int newAck = Mathf.Max(previousAck, ackMsg.stateSequence);
        lastAckedStateSequence[ackMsg.playerId] = newAck;
        
        int statesCleared = 0;
        
        // Remove acknowledged states from queue
        if (unackedStates.ContainsKey(ackMsg.playerId))
        {
            int queueSizeBefore = unackedStates[ackMsg.playerId].Count;
            
            while (unackedStates[ackMsg.playerId].Count > 0)
            {
                PlayerStateMessage oldState = unackedStates[ackMsg.playerId].Peek();
                if (oldState.stateSequence <= ackMsg.stateSequence)
                {
                    unackedStates[ackMsg.playerId].Dequeue();
                    statesCleared++;
                }
                else
                {
                    break; // Keep newer unacked states
                }
            }
            
            int queueSizeAfter = unackedStates[ackMsg.playerId].Count;
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] ✓ Player {ackMsg.playerId} ACKed state {ackMsg.stateSequence} | Cleared {statesCleared} states | Queue: {queueSizeBefore} → {queueSizeAfter} | LastAck: {previousAck} → {newAck}");
        }
        else
        {
            Debug.Log($"[PlayerSpawnManager - SERVER - ACK] ✓ Player {ackMsg.playerId} ACKed state {ackMsg.stateSequence} | No queue found");
        }
        
        // Check for packet loss - if we have many unacked states, might need to resend
        if (unackedStates.ContainsKey(ackMsg.playerId) && unackedStates[ackMsg.playerId].Count > 5)
        {
            Debug.LogWarning($"[PlayerSpawnManager - SERVER - ACK] ⚠ Player {ackMsg.playerId} has {unackedStates[ackMsg.playerId].Count} unacked states - possible packet loss!");
        }
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
            
            // If player was already spawned, update the local flag
            if (spawnedPlayers.TryGetValue(localPlayerId, out GameObject playerObj))
            {
                NetworkPlayer netPlayer = playerObj.GetComponent<NetworkPlayer>();
                NetworkPlayerController controller = playerObj.GetComponent<NetworkPlayerController>();
                NetworkHealth health = playerObj.GetComponent<NetworkHealth>();
                // TODO add other components that need local player flag
                
                if (netPlayer != null)
                    netPlayer.isLocalPlayer = true;
                
                if (controller != null)
                    controller.isLocalPlayer = true;
                
                if (health != null)
                    health.isLocalPlayer = true;
                
                Debug.Log($"[PlayerSpawnManager - Client] Updated player {localPlayerId} to local player");
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
                    // Send periodic update (unreliable for bandwidth efficiency)
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
