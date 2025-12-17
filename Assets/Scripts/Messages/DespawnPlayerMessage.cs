using UnityEngine;

public struct DespawnPlayerMessage : INetworkMessage
{
    [SerializeField]
    public int playerId;

    public DespawnPlayerMessage(int playerId)
    {
        this.playerId = playerId;
    }
}
