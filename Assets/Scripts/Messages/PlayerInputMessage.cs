using UnityEngine;

public struct PlayerInputMessage : INetworkMessage
{
    [SerializeField]
    public int playerId;
    
    [SerializeField]
    public bool forward;
    
    [SerializeField]
    public bool backward;
    
    [SerializeField]
    public bool turnLeft;
    
    [SerializeField]
    public bool turnRight;
    
    [SerializeField]
    public bool anchorToggle;
    
    [SerializeField]
    public float timestamp;
    
    [SerializeField]
    public int sequenceNumber;

    public PlayerInputMessage(int playerId, bool forward, bool backward, bool turnLeft, bool turnRight, 
                             bool anchorToggle, float timestamp, int sequenceNumber)
    {
        this.playerId = playerId;
        this.forward = forward;
        this.backward = backward;
        this.turnLeft = turnLeft;
        this.turnRight = turnRight;
        this.anchorToggle = anchorToggle;
        this.timestamp = timestamp;
        this.sequenceNumber = sequenceNumber;
    }
}