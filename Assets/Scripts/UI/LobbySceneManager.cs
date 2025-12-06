using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySceneManager : MonoBehaviour
{
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
        SceneManager.LoadScene("Game Scene");
    }
}
