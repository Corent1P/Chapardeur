using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
public class UIJoinMenu : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private GameObject LobbyInfoPrefab;
    [SerializeField] private GameObject RefreshButton;
    private List<GameObject> lobbyEntries = new List<GameObject>();

    void Start()
    {
        if (!lobbyManager)
            lobbyManager = FindFirstObjectByType<LobbyManager>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshButton.SetActive(false);
        lobbyManager.ListLobbies();
        StartCoroutine(RefreshCoroutine());
    }

    private IEnumerator RefreshCoroutine()
    {
        yield return new WaitForSeconds(2);
        PopulateLobbies();
        RefreshButton.SetActive(true);
    }

    private async void PopulateLobbies()
    {
        foreach (GameObject entry in lobbyEntries)
        {
            Destroy(entry);
        }
        lobbyEntries.Clear();

        List<Lobby> lobbies = lobbyManager.GetCachedLobbies();
        Debug.Log("Populating lobbies, count: " + lobbies.Count);

        int lobbyCount = 0;
        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies[i];
            if (lobby == null || lobby.Players == null || lobby.Players.Count == 0 || lobby.Players.Count == lobby.MaxPlayers)
                continue;
            GameObject lobbyEntry = Instantiate(LobbyInfoPrefab, transform);
            LobbyInfoUI lobbyInfoUI = lobbyEntry.GetComponent<LobbyInfoUI>();
            if (lobbyInfoUI != null)
            {
                lobbyInfoUI.SetLobbyInfo(lobby, lobbyManager);
            }

            // If this is a UI element, set its RectTransform.anchoredPosition
            RectTransform rt = lobbyEntry.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0, 36 - (lobbyCount * 36));
            }
            else
            {
                // Fallback to localPosition for non-UI objects
                lobbyEntry.transform.localPosition = new Vector3(0, 36 - (lobbyCount * 36), 0);
            }

            lobbyCount++;
            lobbyEntries.Add(lobbyEntry);
            if (lobbyCount >= 4)
                break;
        }
    }
}
