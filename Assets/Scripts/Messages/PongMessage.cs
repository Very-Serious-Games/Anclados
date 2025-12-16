using UnityEngine;

public struct PongMessage : INetworkMessage
{
    [SerializeField]
    public float timestamp;

    public PongMessage(float timestamp)
    {
        this.timestamp = timestamp;
    }
}
