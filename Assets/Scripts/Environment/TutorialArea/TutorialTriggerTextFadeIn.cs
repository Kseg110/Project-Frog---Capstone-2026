using System.Collections;
using UnityEngine;

/// <summary>
/// Fades a popup UI CanvasGroup in when the player is inside the trigger and fades out when they leave.
/// Attach to a GameObject with a trigger Collider (isTrigger=true). Assign a UI GameObject (popupUI)
/// that contains or will get a CanvasGroup component.
/// </summary>
public class TutorialTriggerTextFadeIn : MonoBehaviour
{
    [Tooltip("UI GameObject to fade. Should have or will have a CanvasGroup component.")]
    public GameObject popupUI;

    [Tooltip("Time in seconds for the fade animation.")]
    public float fadeDuration = 0.25f;

    [Tooltip("Start hidden (alpha = 0) on Awake")] 
    public bool startHidden = true;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (popupUI == null)
        {
            Debug.LogWarning($"[TutorialTriggerTextFadeIn] popupUI not assigned on '{gameObject.name}'.");
            return;
        }

        // Ensure the popup is active so CanvasGroup can be created/read
        popupUI.SetActive(true);

        canvasGroup = popupUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = popupUI.AddComponent<CanvasGroup>();

        if (startHidden)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        ShowPopup();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        HidePopup();
    }

    private void ShowPopup()
    {
        if (popupUI == null || canvasGroup == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 1f, fadeDuration));
    }

    private void HidePopup()
    {
        if (popupUI == null || canvasGroup == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, fadeDuration));
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, f));
            canvasGroup.alpha = a;
            yield return null;
        }
        canvasGroup.alpha = to;
        bool visible = to > 0.001f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<PlayerMovement>() != null) return true;
        if (col.GetComponentInParent<PlayerAnchor>() != null) return true;
        if (!string.IsNullOrEmpty(col.tag) && col.CompareTag("Player")) return true;
        return false;
    }
}
