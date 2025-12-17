using UnityEngine;

public struct FireCannonMessage : INetworkMessage
{
    [SerializeField]
    public int playerId;
    
    [SerializeField]
    public bool isLeftCannon;
    
    [SerializeField]
    public Vector3 position;
    
    [SerializeField]
    public Vector3 direction;
    
    [SerializeField]
    public float timestamp;

    public FireCannonMessage(int playerId, bool isLeftCannon, Vector3 position, Vector3 direction, float timestamp)
    {
        this.playerId = playerId;
        this.isLeftCannon = isLeftCannon;
        this.position = position;
        this.direction = direction;
        this.timestamp = timestamp;
    }
}
