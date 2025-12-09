using UnityEngine;

public class ReactorShutdown : MonoBehaviour
{
    [SerializeField] private GameObject[] reactorEnergyCores;
    [SerializeField] private float reactorEnergyCoreSpeed = 3f;
    [SerializeField] private float heightOffset;
    [SerializeField] private ReactorColumnRiser[] reactorColumnRisers;
}
