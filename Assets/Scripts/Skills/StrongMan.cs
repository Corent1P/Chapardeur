using Unity.VisualScripting;
using UnityEngine;

public class StrongMan : ASkills
{
    public float punchRange = 1.5f;
    private void Start()
    {
    }

    void Update()
    {
        if (!isActive)
            return;
    }

    public override void MainAction()
    {
        print("StrongMan Main Action");
        Punch();
    }

    private void Punch()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, punchRange))
        {
            DestructibleWall wall = hit.collider.GetComponent<DestructibleWall>();
            if (wall != null)
            {
                wall.TakeDamage(25, hit.point);
            }
        }
    }

    public override void SecondaryAction()
    {
        print("StrongMan Secondary Action");
    }
}
