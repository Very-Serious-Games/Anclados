using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Queues messages and batches them into packets for efficient transmission.
/// Sends when packet is full or flush interval is reached.
/// </summary>
public class PacketQueue
{
    [Header("Batch Settings")]
    public int maxMessagesPerPacket = 10;
    public float autoFlushInterval = 0.05f; // Not used anymore with fixed tick

    private MessagePacket currentPacket;
    private float timeSinceLastFlush;
    private Action<MessagePacket> onSendPacket;

    public PacketQueue(Action<MessagePacket> sendCallback, int maxMessages = 10, float flushInterval = 0.05f)
    {
        onSendPacket = sendCallback;
        maxMessagesPerPacket = maxMessages;
        autoFlushInterval = flushInterval;
        
        currentPacket = new MessagePacket();
        timeSinceLastFlush = 0f;
    }

    /// <summary>
    /// Enqueue a message to be sent. Will batch with other messages.
    /// </summary>
    public void Enqueue(INetworkMessage message)
    {
        currentPacket.AddMessage(message);
        
        // Auto-flush if packet is full
        if (currentPacket.messageCount >= maxMessagesPerPacket)
        {
            Flush();
        }
    }

    /// <summary>
    /// Force send the current packet immediately
    /// </summary>
    public void Flush()
    {
        if (currentPacket.messageCount > 0)
        {
            onSendPacket?.Invoke(currentPacket);
            currentPacket = new MessagePacket();
            timeSinceLastFlush = 0f;
        }
    }

    /// <summary>
    /// Should be called at network tick rate to handle timed flushes
    /// </summary>
    public void Update()
    {
        // Since we're now called at fixed network tick rate,
        // just flush any pending messages
        if (currentPacket.messageCount > 0)
        {
            Flush();
        }
    }

    public int GetQueuedMessageCount()
    {
        return currentPacket.messageCount;
    }
}
