using UnityEngine;

public struct DamageMessage : INetworkMessage
{
    [SerializeField]
    public int attackerId;
    
    [SerializeField]
    public int targetId;
    
    [SerializeField]
    public float damage;
    
    [SerializeField]
    public Vector3 hitPosition;
    
    [SerializeField]
    public float timestamp;

    public DamageMessage(int attackerId, int targetId, float damage, Vector3 hitPosition, float timestamp)
    {
        this.attackerId = attackerId;
        this.targetId = targetId;
        this.damage = damage;
        this.hitPosition = hitPosition;
        this.timestamp = timestamp;
    }
}
