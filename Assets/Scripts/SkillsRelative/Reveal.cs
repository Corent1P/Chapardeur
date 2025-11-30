using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Reveal : MonoBehaviour
{
    [SerializeField] private Material revealMaterial;
    
    [Header("Reveal Settings")]
    [SerializeField] private float revealPower = 5f;
    [SerializeField] private float revealSoftness = 0.2f;
    [SerializeField] private float distanceAttenuation = 0f;

    private static List<Light> activeRevealLights = new List<Light>();

    public static void RegisterLight(Light light)
    {
        if (!activeRevealLights.Contains(light))
            activeRevealLights.Add(light);
    }

    public static void UnregisterLight(Light light)
    {
        if (activeRevealLights.Contains(light))
            activeRevealLights.Remove(light);
    }

    private void Update()
    {
        if (revealMaterial == null) return;

        Light bestLight = GetBestLight();

        if (bestLight != null)
        {
            revealMaterial.SetFloat("_LightEnabled", 1f);
            revealMaterial.SetVector("_LightPos", bestLight.transform.position);
            revealMaterial.SetVector("_LightDir", -bestLight.transform.forward);
            revealMaterial.SetFloat("_LightAngle", bestLight.spotAngle);
            revealMaterial.SetFloat("_RevealPower", revealPower);
            revealMaterial.SetFloat("_RevealSoftness", revealSoftness);
            revealMaterial.SetFloat("_DistanceAttenuation", distanceAttenuation);
        }
        else
        {
            revealMaterial.SetFloat("_LightEnabled", 0f);
        }
    }

    public bool GetIsIlluminated()
    {
        return GetBestLight() != null;
    }

    private Light GetBestLight()
    {
        Light bestCandidate = null;
        float minDistSq = float.MaxValue;

        foreach (var light in activeRevealLights)
        {
            if (light == null || !light.enabled) continue;

            float distSq = (transform.position - light.transform.position).sqrMagnitude;
            if (distSq > (light.range * light.range)) continue;

            Vector3 toObject = (transform.position - light.transform.position).normalized;
            Vector3 lightDir = light.transform.forward;
            float dotProduct = Vector3.Dot(toObject, lightDir);
            
            float spotHalfAngle = light.spotAngle * 0.5f;
            float threshold = Mathf.Cos(spotHalfAngle * Mathf.Deg2Rad);

            if (dotProduct > threshold)
            {
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    bestCandidate = light;
                }
            }
        }

        return bestCandidate;
    }
}