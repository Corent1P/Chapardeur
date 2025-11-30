using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CoopCamera : MonoBehaviour
{
    [Header("Settings")]
    public float smoothTime = 0.5f;
    public float minZoom = 40f;
    public float maxZoom = 60f;
    public float zoomLimiter = 50f;

    [Header("Debug")]
    [SerializeField] private List<Transform> targets = new List<Transform>();

    private Vector3 offset;
    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        offset = transform.position;
    }

    private void LateUpdate()
    {
        RefreshTargets();

        if (targets.Count == 0) return;

        Move();
        Zoom();
    }

    private void RefreshTargets()
    {
        targets.RemoveAll(t => t == null);

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (var p in players)
        {
            if (!targets.Contains(p.transform))
            {
                targets.Add(p.transform);
            }
        }
    }

    private void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        
        Vector3 newPosition = centerPoint + offset;

        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    private void Zoom()
    {
        float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
    }

    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1)
        {
            return targets[0].position;
        }

        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        return bounds.center;
    }

    private float GetGreatestDistance()
    {
        var bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 0; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }
        return bounds.size.x + bounds.size.z;
    }
}
