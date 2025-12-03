using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class MimeScanner : MonoBehaviour
{
    [Header("Scanner Settings")]
    [SerializeField] private List<Mesh> allowedMeshes = new List<Mesh>();
    [SerializeField] private float scanRange = 5f;
    [SerializeField] private float scanAngle = 60f;

    [Header("Visual Feedback")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material scanningMaterial;
    [SerializeField] private Material successMaterial;
    [SerializeField] private GameObject lightIndicator;

    [Header("Events")]
    public UnityEvent OnValidScan;  // Se dispara cuando se escanea exitosamente

    private MeshRenderer scannerRenderer;
    private List<MimeSkill> nearbyPlayers = new List<MimeSkill>();
    private bool isScanning = false;

    private void Start()
    {
        scannerRenderer = GetComponent<MeshRenderer>();
        if (lightIndicator != null)
            lightIndicator.SetActive(false);
    }

    private void Update()
    {
        if (nearbyPlayers.Count == 0) return;

        foreach (var player in nearbyPlayers)
        {
            if (player == null) continue;

            // Verificar si el jugador está mirando hacia el scanner
            if (IsPlayerFacingScanner(player.transform))
            {
                // Verificar si el mesh actual está en la lista permitida
                MeshFilter playerMeshFilter = player.GetComponent<MeshFilter>();
                if (playerMeshFilter != null && allowedMeshes.Contains(playerMeshFilter.sharedMesh))
                {
                    StartScan(player);
                }
            }
        }
    }

    private bool IsPlayerFacingScanner(Transform player)
    {
        Vector3 directionToScanner = (transform.position - player.position).normalized;
        float angle = Vector3.Angle(player.forward, directionToScanner);
        return angle <= scanAngle;
    }

    private void StartScan(MimeSkill player)
    {
        if (isScanning) return;

        isScanning = true;

        // Feedback visual
        if (scannerRenderer != null && scanningMaterial != null)
            scannerRenderer.material = scanningMaterial;

        if (lightIndicator != null)
            lightIndicator.SetActive(true);

        // Esperar input del jugador (Jump button)
        StartCoroutine(WaitForJumpInput(player));
    }

    private System.Collections.IEnumerator WaitForJumpInput(MimeSkill player)
    {
        bool jumped = false;

        // Esperar máximo 3 segundos por el input
        float timer = 0f;
        while (timer < 3f && !jumped)
        {
            if (Input.GetButtonDown("Jump")) // Verificar configuración del Input System
            {
                jumped = true;
                OnScanSuccess();
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (!jumped)
            OnScanFailed();

        isScanning = false;
    }

    private void OnScanSuccess()
    {
        // Feedback visual
        if (scannerRenderer != null && successMaterial != null)
            scannerRenderer.material = successMaterial;

        // Disparar evento
        OnValidScan?.Invoke();

        // Reset después de 2 segundos
        Invoke("ResetScanner", 2f);
    }

    private void OnScanFailed()
    {
        ResetScanner();
    }

    private void ResetScanner()
    {
        if (scannerRenderer != null && defaultMaterial != null)
            scannerRenderer.material = defaultMaterial;

        if (lightIndicator != null)
            lightIndicator.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MimeSkill mimeSkill = other.GetComponentInChildren<MimeSkill>();
            if (mimeSkill != null && !nearbyPlayers.Contains(mimeSkill))
            {
                nearbyPlayers.Add(mimeSkill);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MimeSkill mimeSkill = other.GetComponentInChildren<MimeSkill>();
            if (mimeSkill != null && nearbyPlayers.Contains(mimeSkill))
            {
                nearbyPlayers.Remove(mimeSkill);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar área de detección
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, scanRange);

        // Dibujar cono de visión
        Vector3 leftBoundary = Quaternion.Euler(0, -scanAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, scanAngle, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * scanRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * scanRange);
    }
}