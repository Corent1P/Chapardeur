using Unity.Netcode;
using UnityEngine;

public class ASkills : NetworkBehaviour, ISkills
{
    [SerializeField] private Mesh AppearanceMesh;
    [SerializeField] private Material AppearanceMaterial;
    protected bool isActive = false;
    protected bool isSkillLocked = false;

    private MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public virtual void ChangeAppearance()
    {
        if (AppearanceMesh != null && AppearanceMaterial != null)
        {
            if (meshFilter != null)
                meshFilter.mesh = AppearanceMesh;

            if (meshRenderer != null)
                meshRenderer.material = AppearanceMaterial;
        }
    }

    public bool IsSkillLocked()
    {
        return isSkillLocked;
    }

    public virtual ISkills ActivateSkill()
    {
        ChangeAppearance();
        isActive = true;

        return this;
    }

    public virtual ISkills DeactivateSkill()
    {
        isActive = false;
        return this;
    }

    public virtual void MainAction()
    {
    }

    public virtual void SecondaryAction()
    {
    }
}
