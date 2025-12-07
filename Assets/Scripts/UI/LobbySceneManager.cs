using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySceneManager : MonoBehaviour
{
    void Start()
    {
        // Subscribe to game state messages on client
        if (GameManager.Instance.gameClient != null)
        {
            GameManager.Instance.gameClient.OnMessageReceived += HandleClientMessage;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance.gameClient != null)
        {
            GameManager.Instance.gameClient.OnMessageReceived -= HandleClientMessage;
        }
    }

    private void HandleClientMessage(INetworkMessage message)
    {
        if (message is GameStateMessage stateMsg)
        {
            if (stateMsg.state == GameState.Playing)
            {
                Debug.Log("[LobbySceneManager - Client] Received game start message, switching to Game Scene");
                SceneManager.LoadScene("Game Scene");
            }
        }
    }

    public void GoToMainMenu()
    {
        if (GameManager.Instance.gameClient != null)
        {
            GameManager.Instance.gameClient.Disconnect();
        }

        if (GameManager.Instance.chatClient != null)
        {
            GameManager.Instance.chatClient.Disconnect();
        }

        if (GameManager.Instance.gameServer != null)
        {
            GameManager.Instance.gameServer.StopServer();
        }

        if (GameManager.Instance.chatServer != null)
        {
            GameManager.Instance.chatServer.StopServer();
        }

        SceneManager.LoadScene("Main Menu Scene");
    }

    public void GoToGame()
    {
        // Only host can start the game
        if (GameManager.Instance.connectionType != ConnectionType.Host)
        {
            Debug.LogWarning("[LobbySceneManager] Only host can start the game!");
            return;
        }

        Debug.Log("[LobbySceneManager - Host] Starting game, broadcasting to clients");
        
        // Broadcast game start message to all clients
        GameStateMessage startMsg = new GameStateMessage(GameState.Playing);
        GameManager.Instance.gameServer.Broadcast(startMsg);

        // Host also switches to game scene
        SceneManager.LoadScene("Game Scene");
    }
}
