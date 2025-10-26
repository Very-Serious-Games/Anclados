
// Abstracts the serialization of messages to/from bytes.
// Useful for especific implementations like JsonNetSerializer, BinarySerializer, etc...
public interface INetworkSerializer
{
    byte[] Serialize<T>(T message) where T : INetworkMessage;
    T Deserialize<T>(byte[] data) where T : INetworkMessage;

    // Added: deserialize to the interface type (serializer should resolve concrete type from payload)
    INetworkMessage Deserialize(byte[] data);
}
