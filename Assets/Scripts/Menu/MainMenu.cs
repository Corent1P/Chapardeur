using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private MenuManager menuManager;
    
    // Référence au nouveau script pour le local (voir point 2)
    [SerializeField] private LocalGameLauncher localGameLauncher; 

    void Start()
    {
        // Si on est en mode "Build Xbox / Offline"
        #if (!DISABLE_ONLINE)
            // On force le bouton à lancer le mode local direct
            playButton.onClick.AddListener(StartLocalGame);
        #else
            // Sinon (PC / Mobile), on lance le menu de connexion normal
            playButton.onClick.AddListener(StartOnlineGame);
        #endif
    }

    private void StartOnlineGame()
    {
        // Ce code n'existera QUE sur PC/Mobile
        #if !(!DISABLE_ONLINE)
            Debug.Log("Mode Online : Affichage Auth...");
            menuManager?.ShowAuthMenu();
        #endif
    }

    private void StartLocalGame()
    {
        // Ce code est valide pour tout le monde (PC peut aussi jouer en local)
        // Mais c'est le SEUL chemin pour la Xbox.
        Debug.Log("Lancement Local...");
        
        // On appelle le nouveau script dédié
        // localGameLauncher.StartLocalSession();
        menuManager?.ShowPlayLocalMenu();
    }
}