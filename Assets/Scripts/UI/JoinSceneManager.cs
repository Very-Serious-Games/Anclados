using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class JoinSceneManager : MonoBehaviour
{
    private String ip;
    private String username;

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }

    public void GoToLobby()
    {
        GameManager.Instance.StartClient(ServerType.UDP);
        GameManager.Instance.gameClient.Connect(ip, 7777);

        GameManager.Instance.StartClient(ServerType.TCP);
        GameManager.Instance.chatClient.Connect(ip, 7778);

        GameManager.Instance.SetUsername(username);

        SceneManager.LoadScene("Lobby Scene");
    }

    public void OnIpTextValueChanged(String text)
    {
        ip = text;
    }

    public void OnUsernameTextValueChanged(String text)
    {
        username = text;
    }
}
