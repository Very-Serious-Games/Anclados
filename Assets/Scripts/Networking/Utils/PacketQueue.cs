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
    public int maxMessagesPerPacket = 10;      // Max messages before auto-send
    public float autoFlushInterval = 0.05f;     // Auto-send every 50ms (20Hz)

    private MessagePacket currentPacket;
    private float lastFlushTime;
    private Action<MessagePacket> onSendPacket;

    public PacketQueue(Action<MessagePacket> sendCallback, int maxMessages = 10, float flushInterval = 0.05f)
    {
        onSendPacket = sendCallback;
        maxMessagesPerPacket = maxMessages;
        autoFlushInterval = flushInterval;
        
        currentPacket = new MessagePacket();
        lastFlushTime = Time.time;
    }

    /// <summary>
    /// Enqueue a message to be sent. Will batch with other messages.
    /// </summary>
    public void Enqueue(INetworkMessage message)
    {
        // Don't batch MessagePackets (to avoid nested packets)
        if (message is MessagePacket)
        {
            onSendPacket?.Invoke(message as MessagePacket);
            return;
        }

        currentPacket.AddMessage(message);

        // Auto-send if packet is full
        if (currentPacket.IsFull(maxMessagesPerPacket))
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
            lastFlushTime = Time.time;
        }
    }

    /// <summary>
    /// Should be called regularly (e.g., in Update) to handle timed flushes
    /// </summary>
    public void Update()
    {
        // Auto-flush if interval elapsed and we have pending messages
        if (currentPacket.messageCount > 0 && 
            Time.time - lastFlushTime >= autoFlushInterval)
        {
            Flush();
        }
    }

    public int GetQueuedMessageCount()
    {
        return currentPacket.messageCount;
    }
}
