using UnityEngine;

[RequireComponent(typeof(AudioSource))] 
public class PuckTracker : MonoBehaviour
{
    [SerializeField] private AirHockeyArenaManager arenaManager;

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip hitSound;  
    [SerializeField] private AudioClip goalSound; 
    [Header("Hit Sound Dynamics")]
    [SerializeField] private float minImpulseForSound = 0.1f; 
    [SerializeField] private float maxImpulseVolume = 5.0f;  

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (arenaManager == null) return;

        AirHockeyAgent agent = collision.collider.GetComponentInParent<AirHockeyAgent>();
        if (agent == null) return;

        PlayHitSound(collision.impulse.magnitude);

        Rigidbody puckRb = GetComponent<Rigidbody>();
        Vector3 puckVelocity = puckRb != null ? puckRb.linearVelocity : Vector3.zero;

        arenaManager.ApplyImpulseReward(agent, puckVelocity);
        arenaManager.SetLastTouch(agent);
    }

    private void PlayHitSound(float impulseMagnitude)
    {
        if (audioSource == null || hitSound == null) return;
        if (impulseMagnitude < minImpulseForSound) return;

        float volume = Mathf.InverseLerp(minImpulseForSound, maxImpulseVolume, impulseMagnitude);

        audioSource.PlayOneShot(hitSound, volume);
    }

    public void PlayGoalSound()
    {
        if (audioSource == null || goalSound == null) return;

        audioSource.PlayOneShot(goalSound, 1.0f);
    }

    public void ResetTracker()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
