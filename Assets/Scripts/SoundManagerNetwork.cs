using UnityEngine;
using Unity.Netcode;

public class NetworkSoundManager : NetworkBehaviour
{
    // public static NetworkSoundManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonPressedSound;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip doorSound;
    [SerializeField] private AudioClip powerDownSound;
    [SerializeField] private AudioClip powerUpSound;
    [SerializeField] private AudioClip alertSound;

    [Header("3D Sound Settings")]
    [SerializeField] private float maxHearingDistance = 30f;
    [SerializeField] private float footstepVolume = 0.5f;

    /// <summary>
    /// Joue un son 2D (UI, etc.) - Local uniquement
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Joue un son 3D à une position dans le monde - Synchronisé réseau
    /// </summary>
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        if (IsServer)
        {
            PlaySoundAtPositionClientRpc(GetClipIndex(clip), position, volume);
        }
        else
        {
            PlaySoundAtPositionServerRpc(GetClipIndex(clip), position, volume);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundAtPositionServerRpc(int clipIndex, Vector3 position, float volume)
    {
        PlaySoundAtPositionClientRpc(clipIndex, position, volume);
    }

    [ClientRpc]
    private void PlaySoundAtPositionClientRpc(int clipIndex, Vector3 position, float volume)
    {
        AudioClip clip = GetClipFromIndex(clipIndex);
        if (clip == null) return;

        // Créer un AudioSource temporaire à la position
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    /// <summary>
    /// Joue un son de pas de parquet aléatoire
    /// </summary>
    public void PlayFootstepSound(Vector3 position)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Choisir un son aléatoire parmi les sons de pas
        AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
        
        // Variation aléatoire de pitch pour plus de réalisme
        float randomPitch = Random.Range(0.9f, 1.1f);

        if (IsServer)
        {
            PlayFootstepClientRpc(GetFootstepIndex(randomFootstep), position, randomPitch);
        }
        else
        {
            PlayFootstepServerRpc(GetFootstepIndex(randomFootstep), position, randomPitch);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayFootstepServerRpc(int footstepIndex, Vector3 position, float pitch)
    {
        PlayFootstepClientRpc(footstepIndex, position, pitch);
    }

    [ClientRpc]
    private void PlayFootstepClientRpc(int footstepIndex, Vector3 position, float pitch)
    {
        if (footstepIndex < 0 || footstepIndex >= footstepSounds.Length) return;

        // Créer un GameObject temporaire pour le son 3D
        GameObject soundObj = new GameObject("Footstep_Sound");
        soundObj.transform.position = position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = footstepSounds[footstepIndex];
        source.volume = footstepVolume;
        source.pitch = pitch;
        source.spatialBlend = 1f; // 3D complet
        source.maxDistance = maxHearingDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        // Détruire après lecture
        Destroy(soundObj, source.clip.length);
    }

    /// <summary>
    /// Joue le son du bouton (local)
    /// </summary>
    public void PlayButtonPressedSound()
    {
        PlaySound(buttonPressedSound);
    }

    #region Helper Methods
    private int GetClipIndex(AudioClip clip)
    {
        // Pour simplifier, on utilise le hash du nom
        return clip.GetInstanceID();
    }

    private AudioClip GetClipFromIndex(int index)
    {
        // Recherche par ID (simplifié, marche pour les clips en Resources)
        if (doorSound != null && doorSound.GetInstanceID() == index) return doorSound;
        if (powerDownSound != null && powerDownSound.GetInstanceID() == index) return powerDownSound;
        if (powerUpSound != null && powerUpSound.GetInstanceID() == index) return powerUpSound;
        if (alertSound != null && alertSound.GetInstanceID() == index) return alertSound;
        if (buttonPressedSound != null && buttonPressedSound.GetInstanceID() == index) return buttonPressedSound;
        
        return null;
    }

    private int GetFootstepIndex(AudioClip clip)
    {
        for (int i = 0; i < footstepSounds.Length; i++)
        {
            if (footstepSounds[i] == clip) return i;
        }
        return 0;
    }

    public void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
    #endregion
}