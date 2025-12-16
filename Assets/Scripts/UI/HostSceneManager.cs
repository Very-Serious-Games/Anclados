using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class HostSceneManager : MonoBehaviour
{
    private String username;

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }

    public void GoToLobby()
    {
        GameManager.Instance.CreateServer(ServerType.UDP);
        GameManager.Instance.gameServer.Start(7777);

        GameManager.Instance.gameServer.OnPlayerConnected += HandlePlayerConnected;

        GameManager.Instance.StartClient(ServerType.UDP);
        GameManager.Instance.gameClient.Connect("127.0.0.1", 7777);

        GameManager.Instance.CreateServer(ServerType.TCP);
        GameManager.Instance.chatServer.Start(7778);

        GameManager.Instance.StartClient(ServerType.TCP);
        GameManager.Instance.chatClient.Connect("127.0.0.1", 7778);

        GameManager.Instance.SetUsername(username);

        SceneManager.LoadScene("Lobby Scene");
    }

    void HandlePlayerConnected(Peer newPeer)
    {
        var joinMessage = new JoinedPlayerMessage(username);
        GameManager.Instance.gameServer.Broadcast(joinMessage);
    }

    public void OnUsernameTextValueChanged(String text)
    {
        username = text;
    }
}
