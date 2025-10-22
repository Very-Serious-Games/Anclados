
// A simple class to hold data about a connected peer.

public class Peer
{
    public int ConnectionId { get; private set; }

    // Here we could add more properties like the name of the player for example.

    public Peer(int connectionId)
    {
        ConnectionId = connectionId;
    }
}
