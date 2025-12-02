using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private GameObject chatPanel;

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();

    void Start()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
    }

    void OnEnable()
    {
        NetworkChatManager.OnMessageReceived += DisplayNewMessage;

        if (sendButton != null) sendButton.onClick.AddListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.AddListener(OnInputSubmit);
    }

    void OnDisable()
    {
        NetworkChatManager.OnMessageReceived -= DisplayNewMessage;

        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.RemoveListener(OnInputSubmit);
    }

    void Update()
    {

    }

    private void OnSendButtonClicked()
    {
        SendMessageFromInput();
    }

    private void OnInputSubmit(string input)
    {
        SendMessageFromInput();
        if (chatInputField != null)
        {
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    private void SendMessageFromInput()
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        chatInputField.text = "";

        // Verificar si NetworkChatManager existe
        if (NetworkChatManager.Instance == null)
        {
            Debug.LogError("NetworkChatManager no encontrado!");
            return;
        }

        // Check for command
        if (message.StartsWith("/sendto "))
        {
            string command = message.Substring("/sendto ".Length);
            int firstQuote = command.IndexOf('"');
            int secondQuote = command.IndexOf('"', firstQuote + 1);

            if (firstQuote != -1 && secondQuote != -1)
            {
                string targetName = command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                string messageContent = command.Substring(secondQuote + 1).Trim();

                if (string.IsNullOrWhiteSpace(messageContent)) return;

                // Enviar mensaje privado
                NetworkChatManager.Instance.SendPrivateMessage(targetName, messageContent);

                // Mostrar localmente inmediatamente
                string localPlayerName = PlayerDataManager.Instance.PlayerName;
                var localMessage = new ChatMessage
                {
                    SenderDisplayName = localPlayerName,
                    RecipientDisplayName = targetName,
                    MessageText = messageContent,
                    IsDirectMessage = true
                };
                DisplayNewMessage(localMessage);
            }
            else
            {
                // Mensaje de error
                var errorMessage = new ChatMessage
                {
                    SenderDisplayName = "System",
                    MessageText = "Formato incorrecto. Usa: /sendto \"Nombre\" mensaje",
                    IsDirectMessage = false
                };
                DisplayNewMessage(errorMessage);
            }
        }
        else
        {
            // Enviar mensaje público
            NetworkChatManager.Instance.SendPublicMessage(message);

            // Mostrar localmente inmediatamente
            string localPlayerName = PlayerDataManager.Instance.PlayerName;
            var localMessage = new ChatMessage
            {
                SenderDisplayName = localPlayerName,
                MessageText = message,
                IsDirectMessage = false
            };
            DisplayNewMessage(localMessage);
        }
    }
    private void DisplayNewMessage(ChatMessage message)
    {
        string localPlayerName = PlayerDataManager.Instance.PlayerName;
        string formattedMessage;

        if (message.IsDirectMessage)
        {
            if (message.SenderDisplayName == localPlayerName)
            {
                formattedMessage = $"<color=#FF6B9D>[Private to {message.RecipientDisplayName}]:</color> {message.MessageText}";
            }
            else
            {
                formattedMessage = $"<color=#6BCEFF>[Private from {message.SenderDisplayName}]:</color> {message.MessageText}";
            }
        }
        else
        {
            if (message.SenderDisplayName == "System")
            {
                formattedMessage = $"<color=#FFA500>[System]: {message.MessageText}</color>";
            }
            else
            {
                formattedMessage = $"<color=#4ECDC4>{message.SenderDisplayName}:</color> {message.MessageText}";
            }
        }

        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = formattedMessage;
        }
        _spawnedChatMessages.Add(messageGO);

     
        if (_spawnedChatMessages.Count > 100)
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        StartCoroutine(ForceScrollDown());
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ToggleChatPanel()
    {
        bool isActive = !chatPanel.activeSelf;
        chatPanel.SetActive(isActive);

        if (isActive)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }
}