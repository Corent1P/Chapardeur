using System;

[Serializable]
public class ChatMessage
{
    public string SenderDisplayName;
    public string SenderPlayerId;
    public string ChannelName;
    public string MessageText;
    public bool IsDirectMessage;
    public string RecipientDisplayName;

    public override string ToString()
    {
        if (IsDirectMessage)
        {
            if (string.IsNullOrEmpty(RecipientDisplayName))
                return $"[Private] {SenderDisplayName}: {MessageText}";
            return $"[Private to {RecipientDisplayName}] {SenderDisplayName}: {MessageText}";
        }
        return $"[All] {SenderDisplayName}: {MessageText}";
    }
}