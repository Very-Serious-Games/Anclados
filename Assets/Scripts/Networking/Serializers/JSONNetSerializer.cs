using System.Text;
using UnityEngine;

public class JSONNetSerializer : INetworkSerializer
{
    // TODO: Think if should recieve a INetworkMessage or just a generic T and convert to INetworkMessage inside
    public byte[] Serialize<T>(T message) where T : INetworkMessage
    {
        var messageJson = JsonUtility.ToJson(message);
        return Encoding.UTF8.GetBytes(messageJson);
    }

    public T Deserialize<T>(byte[] data) where T : INetworkMessage
    {
        var messageJson = Encoding.UTF8.GetString(data);
        return JsonUtility.FromJson<T>(messageJson);
    }
}
