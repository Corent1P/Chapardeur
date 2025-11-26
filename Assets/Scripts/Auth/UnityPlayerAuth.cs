using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using UnityEngine.UI;

public class UnityPlayerAuth : MonoBehaviour
{
    [SerializeField] private Button loginUnityButton;
    [SerializeField] private Button loginAnoButton;
    [SerializeField] private MenuManager menuManager;

    public event Action<PlayerInfo, string> OnSignedIn;
    public event Action<String> OnUpdateName;
    private PlayerInfo playerInfo;

    void OnEnable()
    {
        loginUnityButton?.onClick.AddListener(LoginUnityButton);
        loginAnoButton?.onClick.AddListener(AnonymousLoginButton);
    }

    void OnDisable()
    {
        loginUnityButton?.onClick.RemoveListener(LoginUnityButton);
        loginAnoButton?.onClick.RemoveListener(AnonymousLoginButton);
    }

    private async void LoginUnityButton()
    {
        await InitSignIn();
    }

    private async void AnonymousLoginButton()
    {
        loginAnoButton.interactable = false;
        await SignInAnonymouslyAsync();
        loginAnoButton.interactable = true;
    }

    private async void Start()
    {
        #if DISABLE_ONLINE
            this.enabled = false; 
            return;
        #endif

        await UnityServices.InitializeAsync();
        SetupEvents();
        PlayerAccountService.Instance.SignedIn += SignIn;

        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Already signed in (Auto-Login)");
            await HandleAlreadySignedIn();
        }
    }

    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Player ID " + AuthenticationService.Instance.PlayerId);
            Debug.Log("Access Token " + AuthenticationService.Instance.AccessToken);
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };
        
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player log out");
        };
        
        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session expired");
        };
    }

    private async Task HandleAlreadySignedIn()
    {
        try
        {
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            
            OnSignedIn?.Invoke(playerInfo, name);
            menuManager?.ShowPlayOnlineMenu();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error handling already signed in state: " + ex.Message);
        }
    }

    public async Task InitSignIn()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await HandleAlreadySignedIn();
            return;
        }
        await PlayerAccountService.Instance.StartSignInAsync();
    }

    private async void SignIn()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await HandleAlreadySignedIn();
            return;
        }

        try
        {
            await SignInWithUnityAuth();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private async Task SignInWithUnityAuth()
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            
            playerInfo = AuthenticationService.Instance.PlayerInfo;
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            OnSignedIn?.Invoke(playerInfo, name);
            Debug.Log("Sign In Successful (Unity Account)");
            menuManager?.ShowPlayOnlineMenu();
        }
        catch (AuthenticationException ex)
        {   
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.Log(ex);
        }
    }
    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            // Vérification si déjà connecté
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("User already signed in.");
                await HandleAlreadySignedIn();
                return;
            }

            Debug.Log("Attempting Anonymous Sign In...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign In Anonymous Successful");

            playerInfo = AuthenticationService.Instance.PlayerInfo;

            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            OnSignedIn?.Invoke(playerInfo, name);
            menuManager?.ShowPlayOnlineMenu();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Auth Error: {ex.ErrorCode} - {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request Error: {ex.ErrorCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected Error: {ex.Message}");
        }
    }

    public async Task UpdateName(string newName)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            OnUpdateName?.Invoke(name);
        }
        catch (Exception ex) { Debug.LogError(ex.Message); }
    }

    public async Task DeleteAccountUnityAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        try { await AuthenticationService.Instance.DeleteAccountAsync(); }
        catch (Exception ex) { Debug.LogError(ex.Message); throw; }
    }

    public async void SaveData(string key, string value)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        try
        {
            var playerData = new Dictionary<string, object>() { {key, value} };
            await CloudSaveService.Instance.Data.Player.SaveAsync(playerData);
            Debug.Log($"Data saved: {key}");
        }
        catch (Exception ex) { Debug.LogError(ex.Message); }
    }

    public async void LoadData(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            if (playerData.TryGetValue(key, out var value))
                Debug.Log(key + " value: " + value.Value.GetAs<String>());
            else
                Debug.Log($"No data found for key: {key}");
        }
        catch (Exception ex) { Debug.LogError(ex.Message); }
    }

    public async void DeleteData(string key)
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
            Debug.Log($"Data deleted: {key}");
        }
        catch (Exception ex) { Debug.LogError(ex.Message); }
    }
}