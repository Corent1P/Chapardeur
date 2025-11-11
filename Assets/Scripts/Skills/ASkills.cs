using UnityEngine;

public class ASkills : MonoBehaviour, ISkills
{
    [SerializeField] private Mesh AppearanceMesh;
    [SerializeField] private Material AppearanceMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public virtual void ChangeAppearance()
    {
        if (AppearanceMesh != null && AppearanceMaterial != null)
        {
            if (meshFilter != null)
                meshFilter.mesh = AppearanceMesh;

            if (meshRenderer != null)
                meshRenderer.material = AppearanceMaterial;

            if (meshCollider != null)
                meshCollider.sharedMesh = AppearanceMesh;
        }
    }
    public ISkills ActivateSkill()
    {
        ChangeAppearance();

        return this;
    }

    public virtual void MainAction()
    {
        Debug.Log("Main Action Executed");
    }

    public virtual void SecondaryAction()
    {
        Debug.Log("Secondary Action Executed");
    }
}
