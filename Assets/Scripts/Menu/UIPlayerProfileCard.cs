using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIPlayerProfileCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private RawImage playerAvatarImage;
    [SerializeField] private Texture defaultAvatarTexture;
    [SerializeField] private string defaultPlayerName = "Player";

    private void Awake()
    {
        ResetProfileCard();
    }

    private void SetPlayerName(string playerName)
    {
        if (!string.IsNullOrEmpty(playerName))
        {
            playerNameText.text = playerName;
        }
    }

    private void SetPlayerAvatar(Texture avatarTexture)
    {
        if (avatarTexture != null)
        {
            playerAvatarImage.texture = avatarTexture;
        }
    }

    public void ResetProfileCard()
    {
        UpdateProfileCard(defaultPlayerName, defaultAvatarTexture);
    }

    public void UpdateProfileCard(string playerName = null, Texture avatarTexture = null)
    {
        SetPlayerName(playerName);
        SetPlayerAvatar(avatarTexture);
    }

    public void PlayerIsReady()
    {
        Debug.Log("Player is ready! Sending to server...");
    }
}
