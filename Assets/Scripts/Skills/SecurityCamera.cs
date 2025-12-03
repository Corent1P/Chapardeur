using UnityEngine;
using UnityEngine.Events;

public class SecurityCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField][Range(0, 180)] private float detectionAngle = 60f;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Visual Settings")]
    [SerializeField] private Light cameraLight;
    [SerializeField] private GameObject lightCone;
    [SerializeField] private Color idleColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.red;

    [Header("Animation")]
    [SerializeField] private Animator cameraAnimator;
    [SerializeField] private string deactivateTrigger = "Deactivate";

    [Header("Events")]
    public UnityEvent OnPlayerDetected;
    public UnityEvent OnCameraDeactivated;

    private bool isActive = true;
    private bool playerDetected = false;

    private void Start()
    {
        if (cameraLight != null)
        {
            cameraLight.color = idleColor;
            cameraLight.spotAngle = detectionAngle;
            cameraLight.range = detectionRange;
        }

        if (lightCone != null)
        {
            lightCone.transform.localScale = new Vector3(
                Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad) * detectionRange * 2,
                detectionRange,
                Mathf.Tan(detectionAngle * 0.5f * Mathf.Deg2Rad) * detectionRange * 2
            );
        }
    }

    private void Update()
    {
        if (!isActive) return;

        DetectPlayers();
    }

    private void DetectPlayers()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, detectionLayer);

        bool detectedThisFrame = false;

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                Vector3 directionToPlayer = (col.transform.position - transform.position).normalized;
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

                if (angleToPlayer <= detectionAngle * 0.5f)
                {
                    // Raycast para verificar línea de visión
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            detectedThisFrame = true;

                            if (!playerDetected)
                            {
                                OnPlayerDetected.Invoke();
                                SetAlertState(true);
                            }
                            break;
                        }
                    }
                }
            }
        }

        if (!detectedThisFrame && playerDetected)
        {
            SetAlertState(false);
        }

        playerDetected = detectedThisFrame;
    }

    private void SetAlertState(bool alert)
    {
        if (cameraLight != null)
        {
            cameraLight.color = alert ? alertColor : idleColor;
        }

        if (cameraAnimator != null)
        {
            cameraAnimator.SetBool("Alert", alert);
        }
    }

    public void DeactivateCamera()
    {
        if (!isActive) return;

        isActive = false;
        playerDetected = false;

        // Apagar luz
        if (cameraLight != null)
            cameraLight.enabled = false;

        // Ejecutar animación de desactivación
        if (cameraAnimator != null && !string.IsNullOrEmpty(deactivateTrigger))
        {
            cameraAnimator.SetTrigger(deactivateTrigger);
        }

        // Efecto de explosión/partículas (opcional)
        StartCoroutine(ExplosionEffect());

        // Invocar evento
        OnCameraDeactivated.Invoke();
    }

    private System.Collections.IEnumerator ExplosionEffect()
    {
        // Aquí puedes agregar efectos de partículas
        // GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        // Destroy(explosion, 3f);

        yield return new WaitForSeconds(1f);

        // Desactivar componentes
        if (lightCone != null)
            lightCone.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!isActive) return;

        Gizmos.color = playerDetected ? Color.red : Color.yellow;

        // Dibujar cono de visión
        Vector3 forward = transform.forward * detectionRange;
        Vector3 left = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * forward;
        Vector3 right = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * forward;

        Gizmos.DrawRay(transform.position, forward);
        Gizmos.DrawRay(transform.position, left);
        Gizmos.DrawRay(transform.position, right);

        // Dibujar arco del cono
        int segments = 20;
        Vector3 previousPoint = transform.position + left;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-detectionAngle * 0.5f, detectionAngle * 0.5f, i / (float)segments);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 point = transform.position + direction * detectionRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
}