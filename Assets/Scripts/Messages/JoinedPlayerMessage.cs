using UnityEngine;

public struct JoinedPlayerMessage : INetworkMessage
{
    [SerializeField]
    public string username;
    public JoinedPlayerMessage(string username)
    {
        this.username = username;
    }
}