using System;
using System.IO;
using System.Text;
using UnityEngine;

public class BinaryNetSerializer : INetworkSerializer
{
    public byte[] Serialize(INetworkMessage message)
    {
        if (message == null) return Array.Empty<byte>();

        try
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // Write message type header
                string typeName = message.GetType().AssemblyQualifiedName;
                writer.Write(typeName);

                // Serialize message based on type
                SerializeMessage(writer, message);

                return ms.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BinaryNetSerializer] Serialize error: {e.Message}");
            return Array.Empty<byte>();
        }
    }

    public INetworkMessage Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // Read message type
                string typeName = reader.ReadString();
                Type msgType = Type.GetType(typeName);
                
                if (msgType == null)
                {
                    Debug.LogError($"[BinaryNetSerializer] Type not found: {typeName}");
                    return null;
                }

                // Deserialize message based on type
                return DeserializeMessage(reader, msgType);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BinaryNetSerializer] Deserialize error: {e.Message}");
            return null;
        }
    }

    private void SerializeMessage(BinaryWriter writer, INetworkMessage message)
    {
        // TODO we could optimize sending just the needed fields instead of all fields, also the position can be 2d if y is not used
        switch (message)
        {
            case PlayerInputMessage input:
                writer.Write(input.playerId);
                writer.Write(input.forward);
                writer.Write(input.backward);
                writer.Write(input.turnLeft);
                writer.Write(input.turnRight);
                writer.Write(input.anchorToggle);
                writer.Write(input.fireLeft);
                writer.Write(input.fireRight);
                writer.Write(input.timestamp);
                writer.Write(input.sequenceNumber);
                break;

            case PlayerStateMessage state:
                writer.Write(state.playerId);
                WriteVector3(writer, state.position);
                WriteQuaternion(writer, state.rotation);
                WriteVector3(writer, state.velocity);
                writer.Write(state.anchorActive);
                writer.Write(state.timestamp);
                writer.Write(state.lastProcessedInput);
                break;

            case SpawnPlayerMessage spawn:
                writer.Write(spawn.playerId);
                writer.Write(spawn.username);
                WriteVector3(writer, spawn.spawnPosition);
                WriteQuaternion(writer, spawn.spawnRotation);
                break;

            case DespawnPlayerMessage despawn:
                writer.Write(despawn.playerId);
                break;

            case FireCannonMessage fire:
                writer.Write(fire.playerId);
                writer.Write(fire.isLeftCannon);
                WriteVector3(writer, fire.position);
                WriteVector3(writer, fire.direction);
                writer.Write(fire.timestamp);
                break;

            case SpawnCannonballMessage cannonball:
                writer.Write(cannonball.cannonballId);
                writer.Write(cannonball.ownerId);
                WriteVector3(writer, cannonball.position);
                WriteVector3(writer, cannonball.velocity);
                writer.Write(cannonball.lifetime);
                writer.Write(cannonball.timestamp);
                break;

            case DamageMessage damage:
                writer.Write(damage.attackerId);
                writer.Write(damage.targetId);
                writer.Write(damage.damage);
                WriteVector3(writer, damage.hitPosition);
                writer.Write(damage.timestamp);
                break;

            case PingMessage ping:
                writer.Write(ping.timestamp);
                break;

            case PongMessage pong:
                writer.Write(pong.timestamp);
                break;

            case JoinMessage join:
                writer.Write(join.username);
                break;

            case LobbyJoinMessage lobbyJoin:
                writer.Write(lobbyJoin.username);
                break;

            case JoinedPlayerMessage joined:
                writer.Write(joined.username);
                break;

            case DisconnectedPlayerMessage disconnected:
                writer.Write(disconnected.username);
                break;

            case PlayerChatMessage chat:
                writer.Write(chat.message);
                writer.Write(chat.username);
                writer.Write(chat.timestamp);
                break;

            case GameStateMessage gameState:
                writer.Write((int)gameState.state);
                writer.Write(gameState.countdown);
                writer.Write(gameState.winnerPlayerId);
                writer.Write(gameState.stateData ?? "");
                break;

            case MessagePacket packet:
                writer.Write(packet.messageCount);
                for (int i = 0; i < packet.messageCount; i++)
                {
                    writer.Write(packet.messageTypes[i]);
                    writer.Write(packet.messagePayloads[i]);
                }
                break;

            default:
                Debug.LogError($"[BinaryNetSerializer] Unknown message type: {message.GetType().Name}");
                break;
        }
    }

    private INetworkMessage DeserializeMessage(BinaryReader reader, Type msgType)
    {
        if (msgType == typeof(PlayerInputMessage))
        {
            return new PlayerInputMessage(
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadBoolean(),
                reader.ReadSingle(),
                reader.ReadInt32()
            );
        }
        else if (msgType == typeof(PlayerStateMessage))
        {
            return new PlayerStateMessage(
                reader.ReadInt32(),
                ReadVector3(reader),
                ReadQuaternion(reader),
                ReadVector3(reader),
                reader.ReadBoolean(),
                reader.ReadSingle(),
                reader.ReadInt32()
            );
        }
        else if (msgType == typeof(SpawnPlayerMessage))
        {
            return new SpawnPlayerMessage(
                reader.ReadInt32(),
                reader.ReadString(),
                ReadVector3(reader),
                ReadQuaternion(reader)
            );
        }
        else if (msgType == typeof(DespawnPlayerMessage))
        {
            return new DespawnPlayerMessage(reader.ReadInt32());
        }
        else if (msgType == typeof(FireCannonMessage))
        {
            return new FireCannonMessage(
                reader.ReadInt32(),
                reader.ReadBoolean(),
                ReadVector3(reader),
                ReadVector3(reader),
                reader.ReadSingle()
            );
        }
        else if (msgType == typeof(SpawnCannonballMessage))
        {
            return new SpawnCannonballMessage(
                reader.ReadInt32(),
                reader.ReadInt32(),
                ReadVector3(reader),
                ReadVector3(reader),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }
        else if (msgType == typeof(DamageMessage))
        {
            return new DamageMessage(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadSingle(),
                ReadVector3(reader),
                reader.ReadSingle()
            );
        }
        else if (msgType == typeof(PingMessage))
        {
            return new PingMessage(reader.ReadSingle());
        }
        else if (msgType == typeof(PongMessage))
        {
            return new PongMessage(reader.ReadSingle());
        }
        else if (msgType == typeof(JoinMessage))
        {
            return new JoinMessage(reader.ReadString());
        }
        else if (msgType == typeof(LobbyJoinMessage))
        {
            return new LobbyJoinMessage(reader.ReadString());
        }
        else if (msgType == typeof(JoinedPlayerMessage))
        {
            return new JoinedPlayerMessage(reader.ReadString());
        }
        else if (msgType == typeof(DisconnectedPlayerMessage))
        {
            return new DisconnectedPlayerMessage(reader.ReadString());
        }
        else if (msgType == typeof(PlayerChatMessage))
        {
            return new PlayerChatMessage(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadSingle()
            );
        }
        else if (msgType == typeof(GameStateMessage))
        {
            return new GameStateMessage(
                (GameState)reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                reader.ReadString()
            );
        }
        else if (msgType == typeof(MessagePacket))
        {
            var packet = new MessagePacket();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                packet.messageTypes.Add(reader.ReadString());
                packet.messagePayloads.Add(reader.ReadString());
            }
            packet.messageCount = count;
            return packet;
        }

        Debug.LogError($"[BinaryNetSerializer] Unknown message type: {msgType.Name}");
        return null;
    }

    // Helper methods for Vector3 and Quaternion
    private void WriteVector3(BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    private Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        );
    }

    private void WriteQuaternion(BinaryWriter writer, Quaternion quaternion)
    {
        writer.Write(quaternion.x);
        writer.Write(quaternion.y);
        writer.Write(quaternion.z);
        writer.Write(quaternion.w);
    }

    private Quaternion ReadQuaternion(BinaryReader reader)
    {
        return new Quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        );
    }
}