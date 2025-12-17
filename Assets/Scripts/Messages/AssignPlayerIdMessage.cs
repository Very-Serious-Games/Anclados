using UnityEngine;

public struct AssignPlayerIdMessage : INetworkMessage
{
    [SerializeField]
    public int assignedPlayerId;

    public AssignPlayerIdMessage(int assignedPlayerId)
    {
        this.assignedPlayerId = assignedPlayerId;
    }
}
