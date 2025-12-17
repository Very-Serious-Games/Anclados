using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic ACK (acknowledgment) system for tracking message delivery.
/// Can be used for any message type that needs delivery confirmation.
/// </summary>
public class AckSystem
{
    // Track sequence numbers per identifier (e.g., playerId, entityId, etc.)
    private Dictionary<int, int> currentSequence = new Dictionary<int, int>();
    private Dictionary<int, int> lastAckedSequence = new Dictionary<int, int>();
    
    // Track unacknowledged messages per identifier
    private Dictionary<int, Queue<AckableMessage>> unackedMessages = new Dictionary<int, Queue<AckableMessage>>();
    private const int MAX_UNACKED_MESSAGES = 20; // Limit memory usage
    
    public bool enableLogging = true;
    
    /// <summary>
    /// Structure to hold a message with its sequence number
    /// </summary>
    public struct AckableMessage
    {
        public int sequence;
        public INetworkMessage message;
        public float timestamp;
        
        public AckableMessage(int sequence, INetworkMessage message, float timestamp)
        {
            this.sequence = sequence;
            this.message = message;
            this.timestamp = timestamp;
        }
    }
    
    /// <summary>
    /// Initialize tracking for a new identifier (e.g., new player)
    /// </summary>
    public void InitializeTracking(int identifier)
    {
        if (!currentSequence.ContainsKey(identifier))
        {
            currentSequence[identifier] = 0;
            lastAckedSequence[identifier] = 0;
            unackedMessages[identifier] = new Queue<AckableMessage>();
            
            if (enableLogging)
                Debug.Log($"[AckSystem] Initialized tracking for identifier {identifier}");
        }
    }
    
    /// <summary>
    /// Remove tracking for an identifier (e.g., player disconnected)
    /// </summary>
    public void CleanupTracking(int identifier)
    {
        currentSequence.Remove(identifier);
        lastAckedSequence.Remove(identifier);
        unackedMessages.Remove(identifier);
        
        if (enableLogging)
            Debug.Log($"[AckSystem] Cleaned up tracking for identifier {identifier}");
    }
    
    /// <summary>
    /// Get next sequence number for an identifier and track the message
    /// </summary>
    public int GetNextSequence(int identifier, INetworkMessage message)
    {
        if (!currentSequence.ContainsKey(identifier))
        {
            InitializeTracking(identifier);
        }
        
        // Increment and get sequence
        currentSequence[identifier]++;
        int sequence = currentSequence[identifier];
        
        // Track unacked message
        if (unackedMessages.ContainsKey(identifier))
        {
            // Limit queue size to prevent memory leak
            if (unackedMessages[identifier].Count >= MAX_UNACKED_MESSAGES)
            {
                unackedMessages[identifier].Dequeue();
                if (enableLogging)
                    Debug.LogWarning($"[AckSystem] Too many unacked messages for identifier {identifier}, dropping oldest");
            }
            
            unackedMessages[identifier].Enqueue(new AckableMessage(sequence, message, Time.time));
        }
        
        return sequence;
    }
    
    /// <summary>
    /// Process an acknowledgment for a specific sequence
    /// </summary>
    public void ProcessAck(int identifier, int sequence)
    {
        if (!lastAckedSequence.ContainsKey(identifier))
        {
            InitializeTracking(identifier);
        }
        
        // Update last acknowledged sequence
        int previousAck = lastAckedSequence[identifier];
        int newAck = Mathf.Max(previousAck, sequence);
        lastAckedSequence[identifier] = newAck;
        
        int messagesCleared = 0;
        
        // Remove acknowledged messages from queue
        if (unackedMessages.ContainsKey(identifier))
        {
            int queueSizeBefore = unackedMessages[identifier].Count;
            
            while (unackedMessages[identifier].Count > 0)
            {
                AckableMessage oldMessage = unackedMessages[identifier].Peek();
                if (oldMessage.sequence <= sequence)
                {
                    unackedMessages[identifier].Dequeue();
                    messagesCleared++;
                }
                else
                {
                    break; // Keep newer unacked messages
                }
            }
            
            int queueSizeAfter = unackedMessages[identifier].Count;
            
            if (enableLogging)
            {
                Debug.Log($"[AckSystem] ✓ Identifier {identifier} ACKed sequence {sequence} | " +
                         $"Cleared {messagesCleared} messages | Queue: {queueSizeBefore} → {queueSizeAfter} | " +
                         $"LastAck: {previousAck} → {newAck}");
            }
        }
        
        // Warn about potential packet loss
        if (unackedMessages.ContainsKey(identifier) && unackedMessages[identifier].Count > 10)
        {
            Debug.LogWarning($"[AckSystem] ⚠ Identifier {identifier} has {unackedMessages[identifier].Count} unacked messages - possible packet loss!");
        }
    }
    
    /// <summary>
    /// Get number of unacknowledged messages for an identifier
    /// </summary>
    public int GetUnackedCount(int identifier)
    {
        if (unackedMessages.ContainsKey(identifier))
        {
            return unackedMessages[identifier].Count;
        }
        return 0;
    }
    
    /// <summary>
    /// Get last acknowledged sequence for an identifier
    /// </summary>
    public int GetLastAckedSequence(int identifier)
    {
        if (lastAckedSequence.ContainsKey(identifier))
        {
            return lastAckedSequence[identifier];
        }
        return 0;
    }
    
    /// <summary>
    /// Get current sequence for an identifier
    /// </summary>
    public int GetCurrentSequence(int identifier)
    {
        if (currentSequence.ContainsKey(identifier))
        {
            return currentSequence[identifier];
        }
        return 0;
    }
    
    /// <summary>
    /// Clear all tracking data
    /// </summary>
    public void Clear()
    {
        currentSequence.Clear();
        lastAckedSequence.Clear();
        unackedMessages.Clear();
    }
}
