public struct PlayerChatMessage : INetworkMessage
{
    public string message;
    public string username;
    public float timestamp;

    public PlayerChatMessage(string message, string username, float timestamp)
    {
        this.message = message;
        this.username = username;
        this.timestamp = timestamp;
    }
}