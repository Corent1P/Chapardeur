using UnityEngine;
using UnityEngine.Events;

public class LaserEmitter : MonoBehaviour
{
    [Header("Emitter Settings")]
    [SerializeField] private LaserBeam laserBeam;
    [SerializeField] private bool isActive = true;
    [SerializeField] private float reactivationDelay = 2f;

    [Header("Cut Detection")]
    [SerializeField] private float cutDetectionRadius = 0.5f;

    [Header("Events")]
    public UnityEvent OnLaserCut;

    private void Update()
    {
        if (!isActive || laserBeam == null) return;

        DetectLaserCut();
    }

    private void DetectLaserCut()
    {
        // Crear un rayo esférico a lo largo del láser para detectar cortes
        Vector3 laserEnd = laserBeam.transform.position +
                          laserBeam.transform.TransformDirection(laserBeam.LaserDirection) *
                          laserBeam.maxLaserDistance;

        Vector3 laserMidpoint = (laserBeam.transform.position + laserEnd) * 0.5f;
        float laserLength = Vector3.Distance(laserBeam.transform.position, laserEnd);

        Collider[] colliders = Physics.OverlapCapsule(
            laserBeam.transform.position,
            laserEnd,
            cutDetectionRadius,
            LayerMask.GetMask("Player", "DynamicObject")
        );

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player") || col.CompareTag("Cuttable"))
            {
                CutLaser();
                break;
            }
        }
    }

    private void CutLaser()
    {
        if (!isActive) return;

        isActive = false;
        laserBeam.SetLaserActive(false);

        OnLaserCut.Invoke();

        // Reactivar después de un delay
        if (reactivationDelay > 0)
        {
            Invoke("ReactivateLaser", reactivationDelay);
        }
    }

    private void ReactivateLaser()
    {
        isActive = true;
        laserBeam.SetLaserActive(true);
    }

    public void ToggleLaser(bool active)
    {
        isActive = active;
        if (laserBeam != null)
            laserBeam.SetLaserActive(active);
    }
}