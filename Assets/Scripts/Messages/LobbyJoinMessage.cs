using UnityEngine;

/// <summary>
/// Sent by client when joining the lobby to announce presence via UDP.
/// This ensures the server detects the UDP connection before the game starts.
/// </summary>
[System.Serializable]
public struct LobbyJoinMessage : INetworkMessage
{
    [SerializeField] public string username;

    public LobbyJoinMessage(string username)
    {
        this.username = username;
    }
}
