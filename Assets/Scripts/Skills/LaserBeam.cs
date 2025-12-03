using UnityEngine;
using UnityEngine.Events;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private Vector3 laserDirection = Vector3.forward;
    [SerializeField] private float maxLaserDistance = 50f;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Header("Visual Settings")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private Material laserMaterial;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float laserWidth = 0.1f;

    [Header("Events")]
    public UnityEvent OnLaserTriggered;
    public UnityEvent OnLaserClear;

    private Vector3 currentHitPoint;
    private Collider currentTriggerObject;
    private bool isTriggered = false;

    private void Start()
    {
        InitializeLaserLine();
    }

    private void Update()
    {
        UpdateLaserBeam();
    }

    private void InitializeLaserLine()
    {
        if (laserLine == null)
        {
            laserLine = gameObject.AddComponent<LineRenderer>();
        }

        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.material = laserMaterial;
        laserLine.positionCount = 2;
    }

    private void UpdateLaserBeam()
    {
        Vector3 worldDirection = transform.TransformDirection(laserDirection.normalized);

        RaycastHit hit;
        bool hasHit = Physics.Raycast(transform.position, worldDirection, out hit, maxLaserDistance, obstructionLayer);

        if (hasHit)
        {
            currentHitPoint = hit.point;

            // Verificar si lo que tocó es un objeto que debe activar el láser
            if (hit.collider.CompareTag("Player"))
            {
                if (!isTriggered)
                {
                    isTriggered = true;
                    currentTriggerObject = hit.collider;
                    OnLaserTriggered.Invoke();
                }
            }
            else if (isTriggered)
            {
                isTriggered = false;
                currentTriggerObject = null;
                OnLaserClear.Invoke();
            }

            // Actualizar efecto de impacto
            if (impactEffect != null)
            {
                impactEffect.transform.position = hit.point;
                impactEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                impactEffect.SetActive(true);
            }
        }
        else
        {
            currentHitPoint = transform.position + worldDirection * maxLaserDistance;

            if (isTriggered)
            {
                isTriggered = false;
                currentTriggerObject = null;
                OnLaserClear.Invoke();
            }

            if (impactEffect != null)
                impactEffect.SetActive(false);
        }

        UpdateLaserVisual();
    }

    private void UpdateLaserVisual()
    {
        if (laserLine == null) return;

        laserLine.SetPosition(0, transform.position);
        laserLine.SetPosition(1, currentHitPoint);

        // Cambiar color si está activado
        Color laserColor = isTriggered ? Color.red : Color.green;
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;
    }

    public void SetLaserActive(bool active)
    {
        if (laserLine != null)
            laserLine.enabled = active;

        if (impactEffect != null && !active)
            impactEffect.SetActive(false);

        this.enabled = active;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Vector3 worldDirection = transform.TransformDirection(laserDirection.normalized);
            Gizmos.DrawRay(transform.position, worldDirection * maxLaserDistance);
        }
    }
}