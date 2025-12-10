using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
public class ASkills : NetworkBehaviour, ISkills
{
    [Header("Visuals")]
    // Au lieu du Mesh/Material, on lien le GameObject complet du personnage
    [SerializeField] private GameObject characterModel;
    [SerializeField] private Animator characterAnimator; // L'animator spécifique à ce perso
    [SerializeField] protected Vector3 normalSize = Vector3.one;

    [Header("Settings")]
    [SerializeField] protected Vector3 colliderSize = new Vector3(1f, 2f, 1f); // Pour adapter la hitbox
    [SerializeField] protected Vector3 colliderCenter = new Vector3(0f, 1f, 0f);

    protected bool isActive = false;
    protected bool isSkillLocked = false;
    private CapsuleCollider playerCollider;

    protected GameObject CharacterModel => characterModel;

    private void Awake()
    {
        // On récupère le collider sur le PARENT (le root du player)
        playerCollider = GetComponentInParent<CapsuleCollider>();

        // Sécurité : on s'assure que le modèle est éteint au début
        if(characterModel != null) characterModel.SetActive(false);
    }

    public void UpdateAnimationState(float speed, bool isJumping)
    {
        if (characterAnimator == null || !isActive) return;

        // Paramètres communs à TOUS tes Animator Controllers
        characterAnimator.SetFloat("Speed", speed);
        if(isJumping) characterAnimator.SetTrigger("Jump");
    }

    // Forcer la rotation locale du characterModel à rester à zéro
    // (évite que le Root Motion de l'Animator ne fasse tourner le visuel)
    private void LateUpdate()
    {
        if (isActive && characterModel != null)
        {
            characterModel.transform.localRotation = Quaternion.identity;
        }
    }

    public virtual void ChangeAppearance()
    {
        // if (AppearanceMesh != null && AppearanceMaterial != null)
        // {
        //     if (meshFilter != null)
        //         // meshFilter.mesh = AppearanceMesh;

        //     if (meshRenderer != null)
        //         // meshRenderer.material = AppearanceMaterial;

        //     if (meshCollider != null)
        //         meshCollider.sharedMesh = AppearanceMesh;
        // }
        // if (normalSize != Vector3.zero)
        // {
        //     GetComponent<NetworkTransform>().transform.localScale = normalSize;
        // }
    }

    public bool IsSkillLocked()
    {
        return isSkillLocked;
    }

    public virtual ISkills ActivateSkill()
    {
        isActive = true;
        if (characterModel != null) characterModel.SetActive(true);

        if (playerCollider != null)
        {
            playerCollider.height = colliderSize.y;
            playerCollider.radius = colliderSize.x / 2f;
            playerCollider.center = colliderCenter;
        }

        return this;
    }

    public virtual ISkills DeactivateSkill()
    {
        isActive = false;

        // Désactiver le visuel
        if (characterModel != null) characterModel.SetActive(false);

        return this;
    }

    public virtual void MainAction()
    {
    }

    public virtual void SecondaryAction()
    {
    }
}
