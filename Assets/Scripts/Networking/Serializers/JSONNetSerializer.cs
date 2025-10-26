using System.Text;
using UnityEngine;

public class JSONNetSerializer : INetworkSerializer
{
    // TODO: Think if should recieve a INetworkMessage or just a generic T and convert to INetworkMessage inside
    public byte[] Serialize(INetworkMessage message)
    {
        var messageJson = JsonUtility.ToJson(message);
        return Encoding.UTF8.GetBytes(messageJson);
    }

    public INetworkMessage Deserialize(byte[] data)
    {
        var messageJson = Encoding.UTF8.GetString(data);
        return JsonUtility.FromJson<INetworkMessage>(messageJson);
    }
}
