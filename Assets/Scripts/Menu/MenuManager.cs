using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject playOnlineMenu;
    [SerializeField] private GameObject playLocalMenu;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject usernameMenu;
    [SerializeField] private GameObject authMenu;

    private void Awake()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }
    public void ShowOptionsMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }

    public void ShowPlayOnlineMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(true);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }

    public void ShowPlayLocalMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(true);
    }

    public void ShowJoinMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(true);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }

    public void ShowUsernameMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(true);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }

    public void ShowAuthMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(true);
        playLocalMenu.SetActive(false);
    }

    public void ShowHostMenu()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        hostMenu.SetActive(true);
        playLocalMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void HideAllMenus()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(false);
        playOnlineMenu.SetActive(false);
        joinMenu.SetActive(false);
        hostMenu.SetActive(false);
        usernameMenu.SetActive(false);
        authMenu.SetActive(false);
        playLocalMenu.SetActive(false);
    }
}
