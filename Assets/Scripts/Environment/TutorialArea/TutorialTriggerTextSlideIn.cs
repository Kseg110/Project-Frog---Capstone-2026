using System.Collections;
using UnityEngine;

/// <summary>
/// Slides a popup UI RectTransform up when the player is inside the trigger and down when they leave.
/// Attach to a GameObject with a trigger Collider (isTrigger=true). Assign a UI GameObject (popupUI)
/// that contains a RectTransform. The script will animate the anchoredPosition.
/// </summary>
public class TutorialTriggerTextSlideIn : MonoBehaviour
{
	[Tooltip("UI GameObject to slide. Must have a RectTransform.")]
	public GameObject popupUI;

	[Tooltip("Distance in pixels to slide the UI upward when showing.")]
	public float slideDistance = 100f;

	[Tooltip("Time in seconds for the slide animation.")]
	public float slideDuration = 0.25f;

	[Tooltip("Start hidden (moved down by slideDistance) on Awake")] 
	public bool startHidden = true;

	private RectTransform rect;
	private Vector2 shownPos;
	private Vector2 hiddenPos;
	private Coroutine slideCoroutine;

	private void Awake()
	{
		if (popupUI == null)
		{
			Debug.LogWarning($"[TutorialTriggerTextSlideIn] popupUI not assigned on '{gameObject.name}'.");
			return;
		}

		popupUI.SetActive(true); // ensure active so RectTransform can be read/animated
		rect = popupUI.GetComponent<RectTransform>();
		if (rect == null)
		{
			Debug.LogWarning($"[TutorialTriggerTextSlideIn] popupUI on '{gameObject.name}' has no RectTransform.");
			return;
		}

		shownPos = rect.anchoredPosition;
		hiddenPos = shownPos - new Vector2(0f, slideDistance);

		if (startHidden)
			rect.anchoredPosition = hiddenPos;
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
		if (popupUI == null || rect == null) return;
		popupUI.SetActive(true);
		if (slideCoroutine != null) StopCoroutine(slideCoroutine);
		slideCoroutine = StartCoroutine(SlideRect(rect, rect.anchoredPosition, shownPos, slideDuration));
	}

	private void HidePopup()
	{
		if (popupUI == null || rect == null) return;
		if (slideCoroutine != null) StopCoroutine(slideCoroutine);
		slideCoroutine = StartCoroutine(SlideRect(rect, rect.anchoredPosition, hiddenPos, slideDuration));
	}

	private IEnumerator SlideRect(RectTransform r, Vector2 from, Vector2 to, float duration)
	{
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			float f = Mathf.Clamp01(t / duration);
			r.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, f));
			yield return null;
		}
		r.anchoredPosition = to;
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
