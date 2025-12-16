using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single packet that can contain multiple batched messages.
/// When full or on flush, the packet is sent as a single transmission.
/// </summary>
[Serializable]
public class MessagePacket : INetworkMessage
{
    [SerializeField]
    public List<string> messageTypes;
    
    [SerializeField]
    public List<string> messagePayloads;
    
    [SerializeField]
    public int messageCount;

    public MessagePacket()
    {
        messageTypes = new List<string>();
        messagePayloads = new List<string>();
        messageCount = 0;
    }

    public void AddMessage(INetworkMessage message)
    {
        string typeName = message.GetType().AssemblyQualifiedName;
        string payload = JsonUtility.ToJson(message);
        
        messageTypes.Add(typeName);
        messagePayloads.Add(payload);
        messageCount++;
    }

    public bool IsFull(int maxMessages)
    {
        return messageCount >= maxMessages;
    }

    public void Clear()
    {
        messageTypes.Clear();
        messagePayloads.Clear();
        messageCount = 0;
    }

    public List<INetworkMessage> UnpackMessages()
    {
        List<INetworkMessage> messages = new List<INetworkMessage>();

        for (int i = 0; i < messageCount; i++)
        {
            try
            {
                Type msgType = Type.GetType(messageTypes[i]);
                if (msgType != null)
                {
                    object obj = JsonUtility.FromJson(messagePayloads[i], msgType);
                    if (obj is INetworkMessage msg)
                    {
                        messages.Add(msg);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MessagePacket] Failed to unpack message {i}: {e.Message}");
            }
        }

        return messages;
    }
}
