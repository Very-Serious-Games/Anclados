using UnityEngine;

/// <summary>
/// Sent by client immediately after connecting to announce presence.
/// Required for UDP since it's connectionless - server needs first packet to detect client.
/// </summary>
[System.Serializable]
public struct JoinMessage : INetworkMessage
{
    [SerializeField] public string username;

    public JoinMessage(string username)
    {
        this.username = username;
    }
}
