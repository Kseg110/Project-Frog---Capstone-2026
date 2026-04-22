using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageVignetteEffect : MonoBehaviour
{
    public Image vignetteImage;
    public float flashAlpha = 0.5f;
    public float fadeSpeed = 2.5f;

    private Coroutine fadeRoutine;

    public void TriggerFlash()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        Color c = vignetteImage.color;
        c.a = flashAlpha;
        vignetteImage.color = c;

        while (vignetteImage.color.a > 0)
        {
            c = vignetteImage.color;
            c.a -= fadeSpeed * Time.deltaTime;
            vignetteImage.color = c;
            yield return null;
        }
    }
}