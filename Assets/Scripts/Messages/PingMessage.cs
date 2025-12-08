using UnityEngine;

public struct PingMessage : INetworkMessage
{
    [SerializeField]
    public float timestamp;

    public PingMessage(float timestamp)
    {
        this.timestamp = timestamp;
    }
}
