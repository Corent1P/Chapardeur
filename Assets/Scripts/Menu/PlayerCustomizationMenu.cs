using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;

public class PlayerCustomizationMenu : MonoBehaviour
{
    [Header("Username")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text playerNameDisplay;
    [Header("Avatar")]
    [SerializeField] private Texture[] availableAvatars;
    [SerializeField] private RawImage playerAvatarImage;
    [Header("User Information")]
    [SerializeField] private TMP_InputField birthDay;
    [SerializeField] private TMP_Dropdown birthMonth;
    [SerializeField] private TMP_InputField birthYear;


    private async void Start()
    {
        var playerData = PlayerDataManager.Instance;
        await playerData.LoadCompleteProfile();

        playerNameInput.text = playerData.PlayerName;
        playerNameDisplay.text = playerData.PlayerName;

        int avatarId = playerData.AvatarId;
        if (avatarId >= 0 && avatarId < availableAvatars.Length)
        {
            playerAvatarImage.texture = availableAvatars[avatarId];
        }

        if (playerData.BirthDay != null)
            birthDay.text = playerData.BirthDay;
        if (playerData.BirthMonth != null)
            birthMonth.value = birthMonth.options.FindIndex(option => option.text == playerData.BirthMonth);
        if (playerData.BirthYear != null)
            birthYear.text = playerData.BirthYear;
    }

    public void OnPlayerNameChanged()
    {
        playerNameDisplay.text = playerNameInput.text;
    }

    public void OnAvatarSelected(int avatarIndex)
    {
        if (avatarIndex >= 0 && avatarIndex < availableAvatars.Length)
        {
            playerAvatarImage.texture = availableAvatars[avatarIndex];
        }
    }

    public async void SavePlayerProfile()
    {
        string playerName = playerNameInput.text;
        int avatarId = System.Array.IndexOf(availableAvatars, playerAvatarImage.texture);

        await PlayerDataManager.Instance.SaveProfile(playerName, avatarId, birthDay.text, birthMonth.options[birthMonth.value].text, birthYear.text);
    }
}
