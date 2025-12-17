using System;
using System.IO;
using System.Text;
using UnityEngine;

public class BinaryNetSerializer : INetworkSerializer
{
    // Optimization: Use short instead of int for player IDs (max 32767 players)
    // Use half precision floats where full precision isn't needed
    
    public byte[] Serialize(INetworkMessage message)
    {
        if (message == null) return Array.Empty<byte>();

        try
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // Write message type header as byte enum
                MessageType msgType = GetMessageType(message);
                writer.Write((byte)msgType);

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
                MessageType msgType = (MessageType)reader.ReadByte();
                
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

    private MessageType GetMessageType(INetworkMessage message)
    {
        return message switch
        {
            PlayerInputMessage => MessageType.PlayerInput,
            PlayerStateMessage => MessageType.PlayerState,
            SpawnPlayerMessage => MessageType.SpawnPlayer,
            DespawnPlayerMessage => MessageType.DespawnPlayer,
            FireCannonMessage => MessageType.FireCannon,
            SpawnCannonballMessage => MessageType.SpawnCannonball,
            DamageMessage => MessageType.Damage,
            PingMessage => MessageType.Ping,
            PongMessage => MessageType.Pong,
            JoinMessage => MessageType.Join,
            LobbyJoinMessage => MessageType.LobbyJoin,
            JoinedPlayerMessage => MessageType.JoinedPlayer,
            DisconnectedPlayerMessage => MessageType.DisconnectedPlayer,
            PlayerChatMessage => MessageType.PlayerChat,
            GameStateMessage => MessageType.GameState,
            MessagePacket => MessageType.MessagePacket,
            _ => MessageType.Unknown
        };
    }

    private void SerializeMessage(BinaryWriter writer, INetworkMessage message)
    {
        switch (message)
        {
            case PlayerInputMessage input:
                writer.Write((short)input.playerId);
                // Pack booleans into a single byte (bit flags)
                byte inputFlags = 0;
                if (input.forward) inputFlags |= 0b00000001;
                if (input.backward) inputFlags |= 0b00000010;
                if (input.turnLeft) inputFlags |= 0b00000100;
                if (input.turnRight) inputFlags |= 0b00001000;
                if (input.anchorToggle) inputFlags |= 0b00010000;
                if (input.fireLeft) inputFlags |= 0b00100000;
                if (input.fireRight) inputFlags |= 0b01000000;
                writer.Write(inputFlags);
                WriteHalfFloat(writer, input.timestamp);
                writer.Write((ushort)input.sequenceNumber); // Use ushort for sequence (wraps at 65535)
                break;

            case PlayerStateMessage state:
                writer.Write((short)state.playerId);
                WriteVector2(writer, new Vector2(state.position.x, state.position.z)); // Only X,Z
                WriteCompressedRotation(writer, state.rotation); // Only Y rotation
                WriteVector2(writer, new Vector2(state.velocity.x, state.velocity.z)); // Only X,Z velocity
                writer.Write(state.anchorActive);
                WriteHalfFloat(writer, state.timestamp);
                writer.Write((ushort)state.lastProcessedInput);
                break;

            case SpawnPlayerMessage spawn:
                writer.Write((short)spawn.playerId);
                WriteCompressedString(writer, spawn.username);
                WriteVector2(writer, new Vector2(spawn.spawnPosition.x, spawn.spawnPosition.z));
                WriteCompressedRotation(writer, spawn.spawnRotation);
                break;

            case DespawnPlayerMessage despawn:
                writer.Write((short)despawn.playerId);
                break;

            case FireCannonMessage fire:
                writer.Write((short)fire.playerId);
                writer.Write(fire.isLeftCannon);
                WriteVector2(writer, new Vector2(fire.position.x, fire.position.z));
                WriteVector2Normalized(writer, new Vector2(fire.direction.x, fire.direction.z));
                WriteHalfFloat(writer, fire.timestamp);
                break;

            case SpawnCannonballMessage cannonball:
                writer.Write((short)cannonball.cannonballId);
                writer.Write((short)cannonball.ownerId);
                WriteVector2(writer, new Vector2(cannonball.position.x, cannonball.position.z));
                WriteVector2(writer, new Vector2(cannonball.velocity.x, cannonball.velocity.z));
                WriteHalfFloat(writer, cannonball.lifetime);
                WriteHalfFloat(writer, cannonball.timestamp);
                break;

            case DamageMessage damage:
                writer.Write((short)damage.attackerId);
                writer.Write((short)damage.targetId);
                WriteHalfFloat(writer, damage.damage);
                WriteVector2(writer, new Vector2(damage.hitPosition.x, damage.hitPosition.z));
                WriteHalfFloat(writer, damage.timestamp);
                break;

            case PingMessage ping:
                WriteHalfFloat(writer, ping.timestamp);
                break;

            case PongMessage pong:
                WriteHalfFloat(writer, pong.timestamp);
                break;

            case JoinMessage join:
                WriteCompressedString(writer, join.username);
                break;

            case LobbyJoinMessage lobbyJoin:
                WriteCompressedString(writer, lobbyJoin.username);
                break;

            case JoinedPlayerMessage joined:
                WriteCompressedString(writer, joined.username);
                break;

            case DisconnectedPlayerMessage disconnected:
                WriteCompressedString(writer, disconnected.username);
                break;

            case PlayerChatMessage chat:
                WriteCompressedString(writer, chat.message);
                WriteCompressedString(writer, chat.username);
                WriteHalfFloat(writer, chat.timestamp);
                break;

            case GameStateMessage gameState:
                writer.Write((byte)gameState.state);
                WriteHalfFloat(writer, gameState.countdown);
                writer.Write((short)gameState.winnerPlayerId);
                WriteCompressedString(writer, gameState.stateData ?? "");
                break;

            case MessagePacket packet:
                writer.Write((byte)packet.messageCount);
                for (int i = 0; i < packet.messageCount; i++)
                {
                    WriteCompressedString(writer, packet.messageTypes[i]);
                    WriteCompressedString(writer, packet.messagePayloads[i]);
                }
                break;

            default:
                Debug.LogError($"[BinaryNetSerializer] Unknown message type: {message.GetType().Name}");
                break;
        }
    }

    private INetworkMessage DeserializeMessage(BinaryReader reader, MessageType msgType)
    {
        switch (msgType)
        {
            case MessageType.PlayerInput:
                short playerId = reader.ReadInt16();
                byte inputFlags = reader.ReadByte();
                return new PlayerInputMessage(
                    playerId,
                    (inputFlags & 0b00000001) != 0,
                    (inputFlags & 0b00000010) != 0,
                    (inputFlags & 0b00000100) != 0,
                    (inputFlags & 0b00001000) != 0,
                    (inputFlags & 0b00010000) != 0,
                    (inputFlags & 0b00100000) != 0,
                    (inputFlags & 0b01000000) != 0,
                    ReadHalfFloat(reader),
                    reader.ReadUInt16()
                );

            case MessageType.PlayerState:
                var id = reader.ReadInt16();
                var pos2d = ReadVector2(reader);
                var rot = ReadCompressedRotation(reader);
                var vel2d = ReadVector2(reader);
                var msg = new PlayerStateMessage(
                    id,
                    new Vector3(pos2d.x, 0, pos2d.y),
                    rot,
                    new Vector3(vel2d.x, 0, vel2d.y),
                    reader.ReadBoolean(),
                    ReadHalfFloat(reader),
                    reader.ReadUInt16()
                );
                return msg;

            case MessageType.SpawnPlayer:
                var spawnId = reader.ReadInt16();
                var username = ReadCompressedString(reader);
                var spawnPos = ReadVector2(reader);
                var spawnRot = ReadCompressedRotation(reader);
                return new SpawnPlayerMessage(
                    spawnId,
                    username,
                    new Vector3(spawnPos.x, 0, spawnPos.y),
                    spawnRot
                );

            case MessageType.DespawnPlayer:
                return new DespawnPlayerMessage(reader.ReadInt16());

            case MessageType.FireCannon:
                var fireId = reader.ReadInt16();
                var isLeft = reader.ReadBoolean();
                var firePos = ReadVector2(reader);
                var fireDir = ReadVector2Normalized(reader);
                return new FireCannonMessage(
                    fireId,
                    isLeft,
                    new Vector3(firePos.x, 0, firePos.y),
                    new Vector3(fireDir.x, 0, fireDir.y),
                    ReadHalfFloat(reader)
                );

            case MessageType.SpawnCannonball:
                var cbId = reader.ReadInt16();
                var ownerId = reader.ReadInt16();
                var cbPos = ReadVector2(reader);
                var cbVel = ReadVector2(reader);
                return new SpawnCannonballMessage(
                    cbId,
                    ownerId,
                    new Vector3(cbPos.x, 0, cbPos.y),
                    new Vector3(cbVel.x, 0, cbVel.y),
                    ReadHalfFloat(reader),
                    ReadHalfFloat(reader)
                );

            case MessageType.Damage:
                return new DamageMessage(
                    reader.ReadInt16(),
                    reader.ReadInt16(),
                    ReadHalfFloat(reader),
                    new Vector3(ReadVector2(reader).x, 0, ReadVector2(reader).y),
                    ReadHalfFloat(reader)
                );

            case MessageType.Ping:
                return new PingMessage(ReadHalfFloat(reader));

            case MessageType.Pong:
                return new PongMessage(ReadHalfFloat(reader));

            case MessageType.Join:
                return new JoinMessage(ReadCompressedString(reader));

            case MessageType.LobbyJoin:
                return new LobbyJoinMessage(ReadCompressedString(reader));

            case MessageType.JoinedPlayer:
                return new JoinedPlayerMessage(ReadCompressedString(reader));

            case MessageType.DisconnectedPlayer:
                return new DisconnectedPlayerMessage(ReadCompressedString(reader));

            case MessageType.PlayerChat:
                return new PlayerChatMessage(
                    ReadCompressedString(reader),
                    ReadCompressedString(reader),
                    ReadHalfFloat(reader)
                );

            case MessageType.GameState:
                return new GameStateMessage(
                    (GameState)reader.ReadByte(),
                    ReadHalfFloat(reader),
                    reader.ReadInt16(),
                    ReadCompressedString(reader)
                );

            case MessageType.MessagePacket:
                var packet = new MessagePacket();
                int count = reader.ReadByte();
                for (int i = 0; i < count; i++)
                {
                    packet.messageTypes.Add(ReadCompressedString(reader));
                    packet.messagePayloads.Add(ReadCompressedString(reader));
                }
                packet.messageCount = count;
                return packet;

            default:
                Debug.LogError($"[BinaryNetSerializer] Unknown message type: {msgType}");
                return null;
        }
    }

    // Optimized helper methods
    private void WriteVector2(BinaryWriter writer, Vector2 vector)
    {
        WriteHalfFloat(writer, vector.x);
        WriteHalfFloat(writer, vector.y);
    }

    private Vector2 ReadVector2(BinaryReader reader)
    {
        return new Vector2(
            ReadHalfFloat(reader),
            ReadHalfFloat(reader)
        );
    }

    // Compress rotation to only Y axis
    private void WriteCompressedRotation(BinaryWriter writer, Quaternion rotation)
    {
        // Extract Y rotation and compress to 0-360 range stored as ushort
        float yAngle = rotation.eulerAngles.y;
        ushort compressed = (ushort)(yAngle * 182.04f); // 65535 / 360
        writer.Write(compressed);
    }

    private Quaternion ReadCompressedRotation(BinaryReader reader)
    {
        ushort compressed = reader.ReadUInt16();
        float yAngle = compressed / 182.04f;
        return Quaternion.Euler(0, yAngle, 0);
    }

    // Normalize direction vectors and store as bytes
    private void WriteVector2Normalized(BinaryWriter writer, Vector2 direction)
    {
        direction.Normalize();
        writer.Write((sbyte)(direction.x * 127));
        writer.Write((sbyte)(direction.y * 127));
    }

    private Vector2 ReadVector2Normalized(BinaryReader reader)
    {
        return new Vector2(
            reader.ReadSByte() / 127f,
            reader.ReadSByte() / 127f
        ).normalized;
    }

    // Half precision floats (16-bit)
    private void WriteHalfFloat(BinaryWriter writer, float value)
    {
        writer.Write(Mathf.FloatToHalf(value));
    }

    private float ReadHalfFloat(BinaryReader reader)
    {
        return Mathf.HalfToFloat(reader.ReadUInt16());
    }

    // Compress strings (UTF-8 with length prefix)
    private void WriteCompressedString(BinaryWriter writer, string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            writer.Write((ushort)0);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(str);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }

    private string ReadCompressedString(BinaryReader reader)
    {
        ushort length = reader.ReadUInt16();
        if (length == 0) return "";
        
        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private enum MessageType : byte
    {
        Unknown = 0,
        PlayerInput = 1,
        PlayerState = 2,
        SpawnPlayer = 3,
        DespawnPlayer = 4,
        FireCannon = 5,
        SpawnCannonball = 6,
        Damage = 7,
        Ping = 9,
        Pong = 10,
        Join = 11,
        LobbyJoin = 12,
        JoinedPlayer = 13,
        DisconnectedPlayer = 14,
        PlayerChat = 15,
        GameState = 16,
        MessagePacket = 17
    }
}