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
    }
    
    private void SpawnPlayerForPeer(Peer peer, string username)
    {
        Debug.Log($"[PlayerSpawnManager - Server] SpawnPlayerForPeer called - ConnectionId:{peer.ConnectionId}, PlayerId:{peer.PlayerId}, Username:{username}");
        
        // Check if already spawned
        if (peer.IsPlayerSpawned)
        {
            Debug.LogWarning($"[PlayerSpawnManager - Server] Player {peer.PlayerId} already spawned! Skipping.");
            return;
        }
        
        peer.Username = username;

        // Get spawn position
        Vector3 spawnPos = GetNextSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        // IMPORTANT: Send player ID assignment FIRST
        AssignPlayerIdMessage assignMsg = new AssignPlayerIdMessage(peer.PlayerId);
        gameServer.Send(peer, assignMsg);
        Debug.Log($"[PlayerSpawnManager - Server] Sent AssignPlayerIdMessage({peer.PlayerId}) to connection {peer.ConnectionId}");

        // Spawn player on server
        GameObject playerObj = SpawnPlayerLocal(peer.PlayerId, peer.Username, spawnPos, spawnRot, false);
        peer.PlayerObject = playerObj;
        Debug.Log($"[PlayerSpawnManager - Server] Spawned server-side player object for PlayerId:{peer.PlayerId}");

        // Tell this client about existing players
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

        // Send spawn message to the new player (for themselves)
        SpawnPlayerMessage ownSpawnMsg = new SpawnPlayerMessage(peer.PlayerId, peer.Username, spawnPos, spawnRot);
        gameServer.Send(peer, ownSpawnMsg);
        
        // Broadcast new player to all other clients
        gameServer.Broadcast(ownSpawnMsg, peer);
        
        Debug.Log($"[PlayerSpawnManager - Server] ✓ Completed spawning player {peer.PlayerId} ({peer.Username}) at {spawnPos}");
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
            
            // If player was already spawned, update the local flag
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
                
                // Assign camera to local player
                AssignCameraToLocalPlayer(playerObj);
                
                Debug.Log($"[PlayerSpawnManager - Client] Updated player {localPlayerId} to local player and assigned camera");
            }
        }
        else if (message is SpawnPlayerMessage spawnMsg)
        {
            bool isLocal = (localPlayerId != -1 && spawnMsg.playerId == localPlayerId);
            Debug.Log($"[PlayerSpawnManager - Client] Received SpawnPlayerMessage - PlayerId:{spawnMsg.playerId}, Username:{spawnMsg.username}, IsLocal:{isLocal}, LocalPlayerId:{localPlayerId}");
            
            GameObject spawnedPlayer = SpawnPlayerLocal(spawnMsg.playerId, spawnMsg.username, spawnMsg.spawnPosition, spawnMsg.spawnRotation, isLocal);
            
            // If this was spawned before we got our ID assignment, and now we know it's us, update it
            if (!isLocal && localPlayerId == -1)
            {
                Debug.Log($"[PlayerSpawnManager - Client] Spawned player {spawnMsg.playerId} before receiving local player ID. Will update when ID is assigned.");
            }
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
            Debug.LogWarning($"[PlayerSpawnManager] Player {playerId} already spawned locally! Skipping duplicate spawn.");
            return spawnedPlayers[playerId];
        }

        Debug.Log($"[PlayerSpawnManager] SpawnPlayerLocal - PlayerId:{playerId}, Username:{username}, Position:{position}, IsLocal:{isLocal}");
        
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
        
        // Initialize health
        NetworkHealth health = playerObj.GetComponent<NetworkHealth>();
        if (health != null)
        {
            health.isLocalPlayer = isLocal;
            health.playerId = playerId;
        }
        
        // Handle camera and audio listener
        if (isLocal)
        {
            // Enable camera for local player
            AssignCameraToLocalPlayer(playerObj);
        }
        else
        {
            // Disable camera and audio listener for remote players
            Camera remoteCamera = playerObj.GetComponentInChildren<Camera>(true);
            AudioListener remoteAudioListener = playerObj.GetComponentInChildren<AudioListener>(true);
            
            if (remoteCamera != null)
            {
                remoteCamera.enabled = false;
                remoteCamera.gameObject.SetActive(false);
                Debug.Log($"[PlayerSpawnManager] Disabled camera for remote player: {playerObj.name}");
            }
            
            if (remoteAudioListener != null)
            {
                remoteAudioListener.enabled = false;
                Debug.Log($"[PlayerSpawnManager] Disabled AudioListener for remote player: {playerObj.name}");
            }
        }
        
        return playerObj;
    }

    private void AssignCameraToLocalPlayer(GameObject playerObj)
    {
        if (playerObj == null)
        {
            Debug.LogError("[PlayerSpawnManager] Cannot assign camera - playerObj is null!");
            return;
        }
        
        Debug.Log($"[PlayerSpawnManager] AssignCameraToLocalPlayer called for: {playerObj.name}");
        
        // Find camera in the player's children (since it's a child of the prefab)
        Camera playerCamera = playerObj.GetComponentInChildren<Camera>(true);
        AudioListener audioListener = playerObj.GetComponentInChildren<AudioListener>(true);
        
        Debug.Log($"[PlayerSpawnManager] Found Camera: {(playerCamera != null ? playerCamera.gameObject.name : "NULL")}, AudioListener: {(audioListener != null ? audioListener.gameObject.name : "NULL")}");
        
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.gameObject.SetActive(true);
            Debug.Log($"[PlayerSpawnManager] ✓ Enabled camera for local player: {playerObj.name} (Camera: {playerCamera.gameObject.name})");
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] ✗ Camera not found in player prefab children: {playerObj.name}");
        }
        
        if (audioListener != null)
        {
            audioListener.enabled = true;
            Debug.Log($"[PlayerSpawnManager] ✓ Enabled AudioListener for local player: {playerObj.name}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] ✗ AudioListener not found in player prefab children: {playerObj.name}");
        }
        
        // Also check if there's a CameraManager in the scene that needs updating
        CameraManager cameraManager = FindFirstObjectByType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.target = playerObj.transform;
            Debug.Log($"[PlayerSpawnManager] Assigned CameraManager target to local player: {playerObj.name}");
        }
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
