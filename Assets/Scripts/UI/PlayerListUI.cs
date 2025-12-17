using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
public class PlayerListUI : MonoBehaviour
{

    [Header("UI Elements")]

    public TextMeshProUGUI playerListText;

    void Start()
    {
        GameManager.Instance.gameClient.OnMessageReceived += ManageServerMessages;
    }

    private void ManageServerMessages(INetworkMessage message)
    {
        switch (message)
        {
            case JoinedPlayerMessage joinMessage:
                OnPlayerJoined(joinMessage);
                break;
            case DisconnectedPlayerMessage leftMessage:
                OnPlayerLeft(leftMessage);
                break;
        }
    }
    
    private void OnPlayerJoined(INetworkMessage message)
    {
        if (message is JoinedPlayerMessage joinMessage)
        {
            playerListText.text += $"{joinMessage.username}\n";
        }
    }

    private void OnPlayerLeft(INetworkMessage message)
    {
        if (message is DisconnectedPlayerMessage leftMessage)
        {
            string playerToRemove = $"{leftMessage.username}\n";
            playerListText.text = playerListText.text.Replace(playerToRemove, "");
        }
    }
}
