
// Abstracts the serialization of messages to/from bytes.
// Useful for especific implementations like JsonNetSerializer, BinarySerializer, etc...
public interface INetworkSerializer
{
    byte[] Serialize(INetworkMessage message);
    INetworkMessage Deserialize(byte[] data);
}
