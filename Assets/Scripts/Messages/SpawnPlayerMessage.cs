using UnityEngine;

public struct SpawnPlayerMessage : INetworkMessage
{
    [SerializeField]
    public int playerId;
    
    [SerializeField]
    public string username;
    
    [SerializeField]
    public Vector3 spawnPosition;
    
    [SerializeField]
    public Quaternion spawnRotation;

    public SpawnPlayerMessage(int playerId, string username, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        this.playerId = playerId;
        this.username = username;
        this.spawnPosition = spawnPosition;
        this.spawnRotation = spawnRotation;
    }
}
