using System;

[Serializable]
public class ChatMessage
{
    public string MessageId;
    public string SenderDisplayName;
    public string SenderPlayerId;
    public string MessageText;
    public bool IsDirectMessage;
    public bool IsSystemMessage;
    public string RecipientDisplayName;
    public string RecipientPlayerId;
    public DateTime Timestamp;

    public ChatMessage()
    {
        MessageId = Guid.NewGuid().ToString();
        Timestamp = DateTime.Now;
    }

    public ChatMessage(string senderName, string senderId, string message, bool isDirect = false)
    {
        MessageId = Guid.NewGuid().ToString();
        SenderDisplayName = senderName;
        SenderPlayerId = senderId;
        MessageText = message;
        IsDirectMessage = isDirect;
        IsSystemMessage = false;
        Timestamp = DateTime.Now;
    }
}