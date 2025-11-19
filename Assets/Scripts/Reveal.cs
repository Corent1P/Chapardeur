using UnityEngine;

[ExecuteInEditMode]
public class Reveal : MonoBehaviour
{
    [SerializeField] private Material revealMaterial;
    [SerializeField] private Light revealLight;
    
    [Header("Reveal Settings")]
    [SerializeField] private float revealPower = 10f;
    [SerializeField] private float revealSoftness = 0.2f;
    [SerializeField] private float distanceAttenuation = 0f;

    private void OnEnable()
    {
        if (revealMaterial == null || revealLight == null)
        {
            Debug.LogWarning("Reveal: Assignez le Material et le Light (Spot) dans l'inspecteur!");
        }
    }

    private void Update()
    {
        if (revealMaterial == null || revealLight == null)
            return;

        // Vérifier que c'est un Spot Light
        if (revealLight.type != LightType.Spot)
        {
            Debug.LogWarning("La lumière doit être un Spot Light !");
            return;
        }

        // Vérifier que la lumière est activée
        if (!revealLight.enabled)
        {
            revealMaterial.SetFloat("_LightEnabled", 0f);
            return;
        }

        revealMaterial.SetFloat("_LightEnabled", 1f);
        
        // Mettre à jour les propriétés du shader
        revealMaterial.SetVector("_LightPos", revealLight.transform.position);
        revealMaterial.SetVector("_LightDir", -revealLight.transform.forward);
        revealMaterial.SetFloat("_LightAngle", revealLight.spotAngle);
        revealMaterial.SetFloat("_RevealPower", revealPower);
        revealMaterial.SetFloat("_RevealSoftness", revealSoftness);
        revealMaterial.SetFloat("_DistanceAttenuation", distanceAttenuation);
    }
}