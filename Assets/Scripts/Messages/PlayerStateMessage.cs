using UnityEngine;

public struct PlayerStateMessage : INetworkMessage
{
    [SerializeField]
    public int playerId;
    
    [SerializeField]
    public Vector3 position;
    
    [SerializeField]
    public Quaternion rotation;
    
    [SerializeField]
    public Vector3 velocity;
    
    [SerializeField]
    public bool anchorActive;
    
    [SerializeField]
    public float timestamp;
    
    [SerializeField]
    public int lastProcessedInput;

    public PlayerStateMessage(int playerId, Vector3 position, Quaternion rotation, Vector3 velocity, 
                             bool anchorActive, float timestamp, int lastProcessedInput)
    {
        this.playerId = playerId;
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.anchorActive = anchorActive;
        this.timestamp = timestamp;
        this.lastProcessedInput = lastProcessedInput;
    }
}
