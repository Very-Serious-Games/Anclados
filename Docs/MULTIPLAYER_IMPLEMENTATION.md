# Network Multiplayer Implementation Guide

## 🎮 Complete Multiplayer System

This project now includes a **full server-authoritative multiplayer system** with player state synchronization, combat, and health management.

## 📦 New Components

### Messages (`Assets/Scripts/Messages/`)
- **Player Management**: `PlayerInputMessage`, `PlayerStateMessage`, `SpawnPlayerMessage`, `DespawnPlayerMessage`, `AssignPlayerIdMessage`
- **Combat**: `FireCannonMessage`, `DamageMessage`, `SpawnCannonballMessage`
- **Game State**: `GameStateMessage` with `GameState` enum (Lobby, Starting, Playing, Paused, GameOver)

### Core Components (`Assets/Scripts/Gameplay/`)
- **`NetworkPlayer`**: Identifies players (local vs remote), stores playerId, username
- **`NetworkPlayerController`**: Network-aware player movement with input/state split
- **`PlayerSpawnManager`**: Handles spawning/despawning, player ID assignment, state broadcasting
- **`NetworkHealth`**: Server-authoritative health and damage system

### Utilities (`Assets/Scripts/Networking/Utils/`)
- **`NetLog`**: Centralized logging with categories (Client, Server, Transport, Message, Heartbeat)
- **`NetworkStatsUI`**: Real-time display of RTT, connection status, FPS, player count

### Enhanced Classes
- **`Peer`**: Now includes `PlayerId`, `Username`, `PlayerObject`, `IsPlayerSpawned`
- **`NetworkServer`**: Added `GetConnectedPeers()`, `GetPeer()`, `GetPeerByPlayerId()`, `GetPeerCount()`
- **Transports**: Both UDP and TCP now have `ForceDisconnect(int connectionId)` method

## 🚀 How to Use

### 1. Setup Player Prefab
Your player prefab should have these components:
- `NetworkPlayer`
- `NetworkPlayerController`
- `NetworkHealth`
- `Rigidbody`

### 2. Add PlayerSpawnManager to Scene
In your **Game Scene**, create an empty GameObject and add:
```csharp
PlayerSpawnManager spawnManager = gameObject.AddComponent<PlayerSpawnManager>();
spawnManager.playerPrefab = yourPlayerPrefab;
spawnManager.spawnPoints = yourSpawnPointsArray; // Optional
```

### 3. Add NetworkStatsUI (Optional)
Create a Canvas with TextMeshPro elements and add `NetworkStatsUI` component. Assign the text fields in the Inspector.

### 4. Configure Logging
```csharp
// In your initialization code
NetLog.SetLogLevel(NetLog.LogLevel.Debug); // Or Info, Verbose, etc.
```

## 🎯 Architecture Overview

### Client-Server Flow

**On Player Connect:**
1. Server receives connection → Creates `Peer` → Assigns `PlayerId`
2. Server sends `AssignPlayerIdMessage` to new client
3. Server sends `SpawnPlayerMessage` for all existing players to new client
4. Server broadcasts `SpawnPlayerMessage` for new player to all other clients
5. Client spawns player prefabs locally

**During Gameplay:**
1. **Local Player**: Captures input → Sends `PlayerInputMessage` to server
2. **Server**: Processes inputs → Broadcasts `PlayerStateMessage` at 20Hz
3. **Remote Players**: Receive `PlayerStateMessage` → Apply state via `ApplyNetworkState()`

**Combat:**
1. Player fires cannon → Client sends `FireCannonMessage`
2. Server validates → Broadcasts `SpawnCannonballMessage` to all clients
3. Cannonball hits player → Server calculates damage → Broadcasts `DamageMessage`
4. Clients apply damage locally

**On Disconnect:**
1. Server detects disconnect (heartbeat timeout or TCP close)
2. Server broadcasts `DespawnPlayerMessage`
3. All clients despawn the player object

## 🔧 Configuration

### NetworkPlayerController
- `acceleration`, `maxSpeed`, `reverseSpeed` - Movement physics
- `anchorKey` - Drop/lift anchor (default: F)
- `fireLeftKey`, `fireRightKey` - Fire cannons (default: Z, X)
- `fireCooldown` - Cannon cooldown in seconds

### PlayerSpawnManager
- `playerPrefab` - Prefab to spawn for each player
- `spawnPoints` - Array of spawn point transforms (optional)
- `spawnRadius` - Random spawn radius if no spawn points

### NetworkHealth
- `maxHealth` - Starting health
- `healthBar` - Optional UI slider for health display

## 🐛 Debugging

### Enable Verbose Logging
```csharp
NetLog.EnableVerboseLogging(); // See all messages sent/received
```

### Log Categories
- `NetLog.Client()` - Client-side events
- `NetLog.Server()` - Server-side events
- `NetLog.Transport("UDP", ...)` - Transport layer
- `NetLog.MessageSent()` / `MessageReceived()` - Message tracking
- `NetLog.Heartbeat()` - Ping/pong system

### Check Network Stats
Add `NetworkStatsUI` to see:
- RTT (round-trip time)
- Connection status (Client/Host)
- FPS and packet rate
- Connected player count

## 📝 Known Limitations

1. **Client-Side Prediction**: Currently basic - local player processes input immediately, but no reconciliation with server corrections yet
2. **Lag Compensation**: Shooting doesn't account for network latency
3. **Cannonball Spawning**: `SpawnCannonballMessage` logged but not fully integrated with `CannonBallManager`
4. **Interpolation**: Remote players apply state directly without smoothing (add interpolation for production)
5. **Bandwidth**: State broadcasts every 50ms (20Hz) - optimize with delta compression for production

## 🎓 Next Steps for Production

1. **Add interpolation** to remote player movement
2. **Implement client-side prediction reconciliation** using `lastProcessedInput` sequence numbers
3. **Integrate cannonball spawning** with existing `CannonBallManager`
4. **Add respawn system** for dead players
5. **Implement game state management** (lobby countdown, game over screen)
6. **Add delta compression** to reduce bandwidth
7. **Implement lag compensation** for hit detection

## 🔍 Testing Checklist

- [ ] Host creates game → Client joins → Both players see each other
- [ ] Local player moves → Remote client sees movement
- [ ] Remote player moves → Local client sees movement  
- [ ] Fire cannon → Server spawns cannonball → All clients see it
- [ ] Cannonball hits player → Damage applied → Health decreases
- [ ] Player disconnects → All clients see despawn
- [ ] Heartbeat timeout → Client disconnects after 10s of no response
- [ ] Network stats display RTT and player count correctly
