using System.Collections;
using UnityEngine;

// Fades the player HUD out while a camera pan is active and back in when it ends. -E.M
[RequireComponent(typeof(CanvasGroup))]
public class PlayerHUDPanFader : MonoBehaviour
{
    [Tooltip("Seconds to fade the HUD out when a pan starts.")]
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Tooltip("Seconds to fade the HUD back in when a pan ends.")]
    [SerializeField] private float fadeInDuration = 0.35f;

    [Tooltip("Alpha the HUD fades to during a pan. 0 = fully hidden.")]
    [SerializeField] private float hiddenAlpha = 0f;

    [Tooltip("Disable the GameObject once fully faded out (saves layout/raycast cost). Re-enabled before fading back in.")]
    [SerializeField] private bool disableWhenHidden = true;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    // Tracks the last pan state we reacted to, so we only fire on transitions.
    private bool wasPanActive;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // Sync to whatever the current pan state is when we come online.
        wasPanActive = CameraPanEffect.GlobalPanActive;
        ApplyImmediate(wasPanActive);
    }

    private void Update()
    {
        bool panActive = CameraPanEffect.GlobalPanActive;

        if (panActive == wasPanActive)
            return;

        wasPanActive = panActive;

        if (panActive)
            BeginFade(hiddenAlpha, fadeOutDuration, hideAfter: disableWhenHidden);
        else
            BeginFade(1f, fadeInDuration, hideAfter: false);
    }

    private void BeginFade(float targetAlpha, float duration, bool hideAfter)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        // If we're fading back in, make sure the GameObject is live first.
        if (!hideAfter && !gameObject.activeSelf)
            gameObject.SetActive(true);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, hideAfter));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool hideAfter)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        // Block input the moment a fade-out begins; only re-enable interaction on a fade-in.
        bool interactable = targetAlpha > 0.99f;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        if (hideAfter)
            gameObject.SetActive(false);

        fadeRoutine = null;
    }

    // Snaps to a state with no animation — used on enable so we don't flash a frame of the wrong alpha.
    private void ApplyImmediate(bool panActive)
    {
        float alpha = panActive ? hiddenAlpha : 1f;
        canvasGroup.alpha = alpha;

        bool interactable = !panActive;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;

        if (disableWhenHidden && panActive && alpha <= 0.001f)
            gameObject.SetActive(false);
    }
}