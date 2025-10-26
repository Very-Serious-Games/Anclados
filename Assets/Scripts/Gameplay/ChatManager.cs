using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField messageInputField;
    public Button sendButton;
    public ScrollRect chatScrollRect;
    public TextMeshProUGUI chatText;

    [Header("Settings")]
    public int maxMessages = 100;
    private string username;
    private List<string> chatMessages = new List<string>();
    GameManager gameManager = GameManager.Instance;
    private void Start()
    {
        // Get username
        username = gameManager.GetUsername();

        // Subscribe to network events
        SubscribeToNetworkEvents();
    }

    private void SubscribeToNetworkEvents()
    {
        if (gameManager.chatClient != null)
        {
            gameManager.chatClient.OnMessageReceived += HandleDataReceivedFromServer;
        }

        if (gameManager.chatServer != null)
        {
            gameManager.chatServer.OnMessageReceived += HandleMessageReceived;
        }
    }

    public void SendMessage()
    {
        string messageText = messageInputField.text.Trim();
        PlayerChatMessage chatMessage = new PlayerChatMessage(messageText, username, Time.time);

        gameManager.chatClient.Send(chatMessage);
        DisplayMessage(chatMessage);

        // Clear input field
        messageInputField.text = "";
        messageInputField.ActivateInputField();
    }

    private void HandleDataReceivedFromServer(INetworkMessage message)
    {
        if (message is PlayerChatMessage chatMessage)
        {
            DisplayMessage(chatMessage);
        }
    }

    private void HandleMessageReceived(Peer sender, INetworkMessage message)
    {
        if (message is PlayerChatMessage chatMessage)
        {
            // Broadcast to all clients except the sender
            if (gameManager.chatServer != null)
            {
                // TODO do something like: 
                // gameManager.chatServer.Broadcast(chatMessage, excludePeer: sender);

                // For now, just broadcast to all
                gameManager.chatServer.Broadcast(chatMessage);
            }
        }
    }

    private void DisplayMessage(PlayerChatMessage chatMessage)
    {
        // Setup message text
        string formattedMessage = $"[{chatMessage.timestamp:F2}] {chatMessage.username}: {chatMessage.message}";
    
        chatMessages.Add(formattedMessage);
    
        // Maintain max messages
        while (chatMessages.Count > maxMessages)
        {
            chatMessages.RemoveAt(0);
        }
    
        // Update UI text - join all messages with newlines
        if (chatText != null)
        {
            chatText.text = string.Join("\n", chatMessages);
        }
    
        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from network events
        if (gameManager.chatClient != null)
        {
            gameManager.chatClient.OnMessageReceived -= HandleDataReceivedFromServer;
        }

        if (gameManager.chatServer != null)
        {
            gameManager.chatServer.OnMessageReceived -= HandleMessageReceived;
        }
    }
}