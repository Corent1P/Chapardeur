using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button toggleChatButton;
    [SerializeField] private TMP_Dropdown channelDropdown;
    [SerializeField] private TMP_InputField privateMessageInput;
    [SerializeField] private TMP_InputField targetPlayerInput;

    [Header("Prefabs")]
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject privateMessagePrefab;

    private VivoxManager vivoxManager;
    private bool isChatVisible = true;

    private void Start()
    {
        vivoxManager = FindObjectOfType<VivoxManager>();

        sendButton.onClick.AddListener(SendMessage);
        toggleChatButton.onClick.AddListener(ToggleChat);
        messageInput.onSubmit.AddListener((text) => SendMessage());

        channelDropdown.onValueChanged.AddListener(OnChannelChanged);

        InitializeChannels();
    }

    private void InitializeChannels()
    {
        channelDropdown.ClearOptions();
        var channels = new List<string> { "Lobby", "Global", "Team" };
        channelDropdown.AddOptions(channels);
    }

    public void AddMessageToChat(string sender, string message, bool isPrivate = false)
    {
        GameObject messageObj = Instantiate(isPrivate ? privateMessagePrefab : messagePrefab, messageContainer);
        TMP_Text messageText = messageObj.GetComponent<TMP_Text>();

        string formattedMessage = isPrivate ?
            $"[PRIVADO] {sender}: {message}" :
            $"[{GetCurrentChannel()}] {sender}: {message}";

        messageText.text = formattedMessage;

        Canvas.ForceUpdateCanvases();
    }

    private void SendMessage()
    {
        if (string.IsNullOrEmpty(messageInput.text)) return;

        string currentChannel = GetCurrentChannel();
        _ = vivoxManager.SendMessageToChannel(messageInput.text, currentChannel);

        messageInput.text = "";
        messageInput.ActivateInputField();
    }

    public void SendPrivateMessage()
    {
        if (string.IsNullOrEmpty(privateMessageInput.text) || string.IsNullOrEmpty(targetPlayerInput.text))
            return;

        _ = vivoxManager.SendDirectMessage(privateMessageInput.text, targetPlayerInput.text);

        privateMessageInput.text = "";
        privateMessageInput.ActivateInputField();
    }

    private void ToggleChat()
    {
        isChatVisible = !isChatVisible;
        chatPanel.SetActive(isChatVisible);
        toggleChatButton.GetComponentInChildren<TMP_Text>().text = isChatVisible ? "Ocultar Chat" : "Mostrar Chat";
    }

    private void OnChannelChanged(int index)
    {
        string channelName = channelDropdown.options[index].text;
    }

    private string GetCurrentChannel()
    {
        return channelDropdown.options[channelDropdown.value].text;
    }

    private void OnDestroy()
    {
        sendButton.onClick.RemoveAllListeners();
        toggleChatButton.onClick.RemoveAllListeners();
        messageInput.onSubmit.RemoveAllListeners();
        channelDropdown.onValueChanged.RemoveAllListeners();
    }
}