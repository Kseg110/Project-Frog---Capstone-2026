using UnityEngine;
using System.Collections;

public class LensFlareEffect : MonoBehaviour
{
    public ParticleSystem particleSystemToPlay;
    public Light flareLight;
    public float lightDuration = 0.08f;

    public void PlayFlare()
    {
        if (particleSystemToPlay != null)
        {
            particleSystemToPlay.Play();
        }

        if (flareLight != null)
        {
            StartCoroutine(FlareLightRoutine());
        }
    }

    private IEnumerator FlareLightRoutine()
    {
        flareLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        flareLight.enabled = false;
    }
}