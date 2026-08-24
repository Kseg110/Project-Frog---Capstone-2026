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

    [Header("Backup timing")]
    [Tooltip("Auto-hide popup after this many seconds if it doesn't disappear")]
    public float autoHideDuration = 10f;

    [Tooltip("Delay after an auto-hide before the player can reactivate the popup")]
    public float reactivateDelay = 5f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    // new
    private Coroutine autoHideCoroutine;
    private Coroutine reactivateCoroutine;
    private bool canReactivate = true;

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
        // Player leaving should hide immediately and NOT start reactivate cooldown
        HidePopup(startReactivateCooldown: false);
    }

    private void ShowPopup()
    {
        if (popupUI == null || canvasGroup == null) return;

        // If we're in reactivation cooldown, ignore attempts to show
        if (!canReactivate) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 1f, fadeDuration));

        // restart auto-hide timer whenever popup is shown
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        autoHideCoroutine = StartCoroutine(AutoHideTimer());
    }

    private void HidePopup(bool startReactivateCooldown = false)
    {
        if (popupUI == null || canvasGroup == null) return;

        // Stop pending auto-hide if hiding manually
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, fadeDuration));

        // If this hide was triggered by the auto-hide timer, start a reactivation cooldown
        if (startReactivateCooldown)
        {
            if (reactivateCoroutine != null)
            {
                StopCoroutine(reactivateCoroutine);
                reactivateCoroutine = null;
            }
            reactivateCoroutine = StartCoroutine(ReactivateCooldown());
        }
    }

    private IEnumerator AutoHideTimer()
    {
        yield return new WaitForSeconds(autoHideDuration);

        // If popup is still visible, auto-hide and start reactivate cooldown
        if (canvasGroup != null && canvasGroup.alpha > 0.001f)
        {
            HidePopup(startReactivateCooldown: true);
        }

        autoHideCoroutine = null;
    }

    private IEnumerator ReactivateCooldown()
    {
        canReactivate = false;
        yield return new WaitForSeconds(reactivateDelay);
        canReactivate = true;
        reactivateCoroutine = null;
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
