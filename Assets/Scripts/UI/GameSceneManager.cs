using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public void GoToLobby()
    {
        SceneManager.LoadScene("Lobby Scene");
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
            GameManager.Instance.gameServer.Stop();
        }

        if (GameManager.Instance.chatServer != null)
        {
            GameManager.Instance.chatServer.Stop();
        }

        SceneManager.LoadScene("Main Menu Scene");
    }

    public void ExitGame()
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
            GameManager.Instance.gameServer.Stop();
        }

        if (GameManager.Instance.chatServer != null)
        {
            GameManager.Instance.chatServer.Stop();
        }

        Application.Quit();
    }
}
