using System;

[Serializable]
public class StateAckMessage : INetworkMessage
{
    public int playerId;
    public int stateSequence;

    public StateAckMessage(int playerId, int stateSequence)
    {
        this.playerId = playerId;
        this.stateSequence = stateSequence;
    }
}
