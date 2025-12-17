using UnityEngine;

public struct PlayerChatMessage : INetworkMessage
{
    [SerializeField]
    public string message;
    [SerializeField]
    public string username;
    [SerializeField]
    public float timestamp;

    public PlayerChatMessage(string message, string username, float timestamp)
    {
        this.message = message;
        this.username = username;
        this.timestamp = timestamp;
    }
}