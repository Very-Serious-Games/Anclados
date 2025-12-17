using UnityEngine;

public enum GameState
{
    Lobby,
    Starting,
    Playing,
    Paused,
    GameOver
}

public struct GameStateMessage : INetworkMessage
{
    [SerializeField]
    public GameState state;
    
    [SerializeField]
    public float countdown;
    
    [SerializeField]
    public int winnerPlayerId;
    
    [SerializeField]
    public string stateData;

    public GameStateMessage(GameState state, float countdown = 0f, int winnerPlayerId = -1, string stateData = "")
    {
        this.state = state;
        this.countdown = countdown;
        this.winnerPlayerId = winnerPlayerId;
        this.stateData = stateData;
    }
}
