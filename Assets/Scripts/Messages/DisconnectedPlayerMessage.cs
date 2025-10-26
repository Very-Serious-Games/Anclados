using UnityEngine;

public struct DisconnectedPlayerMessage : INetworkMessage
{
    [SerializeField]
    public string username;
    public DisconnectedPlayerMessage(string username)
    {
        this.username = username;
    }
}