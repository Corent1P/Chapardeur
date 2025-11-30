using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string PlayerName { get; private set; } = "New Player";
    public int AvatarId { get; private set; } = 0;
    public string BirthDay { get; private set; } = "01";
    public string BirthMonth { get; private set; } = "January";
    public string BirthYear { get; private set; } = "2000";

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
        
        DontDestroyOnLoad(this);
    }

    public async Task SaveProfile(string name, int avatarId, string birthDay, string birthMonth, string birthYear)
    {
        PlayerName = name;
        AvatarId = avatarId;

        var data = new Dictionary<string, object>
        {
            { "PlayerName", name },
            { "AvatarId", avatarId },
            { "BirthDay", birthDay },
            { "BirthMonth", birthMonth },
            { "BirthYear", birthYear }
        };

        try {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Profil sauvegardé dans le Cloud !");
        } catch (System.Exception e) {
            Debug.LogError($"Erreur Cloud Save: {e}");
        }
    }

    public async Task LoadProfile()
    {
        try {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "PlayerName", "AvatarId" });

            if (data.TryGetValue("PlayerName", out var nameItem))
                PlayerName = nameItem.Value.GetAs<string>();

            if (data.TryGetValue("AvatarId", out var avatarItem))
                AvatarId = avatarItem.Value.GetAs<int>();

            Debug.Log($"Profil chargé : {PlayerName}, Avatar: {AvatarId}");
        } catch (System.Exception e) {
            Debug.LogWarning($"Pas de sauvegarde trouvée ou erreur (C'est normal au premier lancement): {e}");
        }
    }

    public async Task LoadCompleteProfile()
    {
        try {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "PlayerName", "AvatarId", "BirthDay", "BirthMonth", "BirthYear" });

            if (data.TryGetValue("PlayerName", out var nameItem))
                PlayerName = nameItem.Value.GetAs<string>();

            if (data.TryGetValue("AvatarId", out var avatarItem))
                AvatarId = avatarItem.Value.GetAs<int>();

            if (data.TryGetValue("BirthDay", out var dayItem))
                BirthDay = dayItem.Value.GetAs<string>();

            if (data.TryGetValue("BirthMonth", out var monthItem))
                BirthMonth = monthItem.Value.GetAs<string>();

            if (data.TryGetValue("BirthYear", out var yearItem))
                BirthYear = yearItem.Value.GetAs<string>();

            Debug.Log($"Profil complet chargé : {PlayerName}, Avatar: {AvatarId}");
        } catch (System.Exception e) {
            Debug.LogWarning($"Pas de sauvegarde trouvée ou erreur (C'est normal au premier lancement): {e}");
        }
    }
}