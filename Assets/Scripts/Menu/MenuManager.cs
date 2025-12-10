using UnityEngine;
using UnityEngine.EventSystems;

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

    [SerializeField] private GameObject mainMenuFirstButton;
    [SerializeField] private GameObject optionsMenuFirstButton;
    [SerializeField] private GameObject playOnlineMenuFirstButton;
    [SerializeField] private GameObject playLocalMenuFirstButton;
    [SerializeField] private GameObject joinMenuFirstButton;
    [SerializeField] private GameObject hostMenuFirstButton;
    [SerializeField] private GameObject usernameMenuFirstButton;
    [SerializeField] private GameObject authMenuFirstButton;

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
        EventSystem.current.SetSelectedGameObject(mainMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(optionsMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(playOnlineMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(playLocalMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(joinMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(usernameMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(authMenuFirstButton);
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
        EventSystem.current.SetSelectedGameObject(hostMenuFirstButton);
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
