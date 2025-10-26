using System;
using System.Text;
using UnityEngine;

public class JSONNetSerializer : INetworkSerializer
{
    [Serializable]
    private class Envelope
    {
        public string Type;
        public string Payload;
    }

    public byte[] Serialize(INetworkMessage message)
    {
        if (message == null) return Array.Empty<byte>();

        try
        {
            var payloadJson = JsonUtility.ToJson(message);
            var env = new Envelope
            {
                Type = message.GetType().AssemblyQualifiedName,
                Payload = payloadJson
            };
            var envelopeJson = JsonUtility.ToJson(env);
            return Encoding.UTF8.GetBytes(envelopeJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSONNetSerializer] Serialize error: {e.Message}");
            return Array.Empty<byte>();
        }
    }

    public INetworkMessage Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        try
        {
            var json = Encoding.UTF8.GetString(data);
            var env = JsonUtility.FromJson<Envelope>(json);
            if (env == null || string.IsNullOrEmpty(env.Type))
            {
                Debug.LogError("[JSONNetSerializer] Envelope missing type information.");
                return null;
            }

            var msgType = Type.GetType(env.Type);
            if (msgType == null)
            {
                Debug.LogError($"[JSONNetSerializer] Type not found: {env.Type}");
                return null;
            }

            object obj = JsonUtility.FromJson(env.Payload, msgType);
            if (obj == null)
            {
                Debug.LogError($"[JSONNetSerializer] Failed to deserialize payload to {msgType.FullName}");
                return null;
            }

            return (INetworkMessage)obj;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSONNetSerializer] Deserialize error: {e.Message}");
            return null;
        }
    }
}