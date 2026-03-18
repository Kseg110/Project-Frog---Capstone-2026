using UnityEngine;

public class AnchorVFX : MonoBehaviour
{
    [Header("Idle Effect")]
    [SerializeField] private ParticleSystem idleParticles;

    [Header("Activation Effect")]
    [SerializeField] private ParticleSystem activationParticles;

    private void Start()
    {
        if (idleParticles != null)
            idleParticles.Play();
    }

    public void PlayActivation()
    {
        if (idleParticles != null)
            idleParticles.Stop();

        if (activationParticles != null)
        {
            activationParticles.Stop();
            activationParticles.Clear();
            activationParticles.Play();
        }
    }

    public void StopActivation()
    {
        if (activationParticles != null)
        {
            activationParticles.Stop();
            activationParticles.Clear();
        }

        if (idleParticles != null)
            idleParticles.Play();
    }
}