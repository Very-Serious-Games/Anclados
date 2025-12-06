using UnityEngine;

public struct SpawnCannonballMessage : INetworkMessage
{
    [SerializeField]
    public int cannonballId;
    
    [SerializeField]
    public int ownerId;
    
    [SerializeField]
    public Vector3 position;
    
    [SerializeField]
    public Vector3 velocity;
    
    [SerializeField]
    public float lifetime;
    
    [SerializeField]
    public float timestamp;

    public SpawnCannonballMessage(int cannonballId, int ownerId, Vector3 position, Vector3 velocity, float lifetime, float timestamp)
    {
        this.cannonballId = cannonballId;
        this.ownerId = ownerId;
        this.position = position;
        this.velocity = velocity;
        this.lifetime = lifetime;
        this.timestamp = timestamp;
    }
}
