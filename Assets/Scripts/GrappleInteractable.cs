using UnityEngine;
using UnityEngine.Events;

public class GrappleInteractable : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Force nécessaire pour activer (ex: 50).")]
    [SerializeField] private float activationThreshold = 50f; 
    
    [Tooltip("Si faux, l'objet se désactive quand on relâche la tension (ex: porte maintenue ouverte).")]
    [SerializeField] private bool toggleMode = false;

    [Header("Réactions")]
    public UnityEvent OnTensionStart;   // La porte s'ouvre
    public UnityEvent OnTensionStop;    // La porte se ferme
    public UnityEvent<float> OnTensionUpdate; // Pour animer un levier progressivement

    private bool isActivated = false;

    // Appelé par le GrapplingHook via la physique
    public void ApplyTension(float currentForce)
    {
        OnTensionUpdate.Invoke(currentForce);

        if (currentForce >= activationThreshold)
        {
            if (!isActivated)
            {
                isActivated = true;
                OnTensionStart.Invoke();
            }
        }
        else if (!toggleMode && isActivated)
        {
            // La tension a baissé sous le seuil -> on relâche
            Release();
        }
    }

    public void OnGrappleDetach()
    {
        OnTensionUpdate.Invoke(0f);
        if (!toggleMode && isActivated)
        {
            Release();
        }
    }

    private void Release()
    {
        isActivated = false;
        OnTensionStop.Invoke();
    }
}