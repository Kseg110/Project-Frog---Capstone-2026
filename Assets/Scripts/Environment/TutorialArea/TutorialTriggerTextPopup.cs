using UnityEngine;

/// <summary>
/// Shows a popup UI GameObject while the player stands inside this trigger, hides when they leave.
/// Attach this to a GameObject with a trigger Collider (isTrigger=true).
/// </summary>
public class TutorialTriggerTextPopup : MonoBehaviour
{
    [Tooltip("The UI GameObject (Canvas or panel) to enable while the player is inside the trigger.")]
    public GameObject popupUI;

    [Tooltip("Tag used to identify the player. If the collider's root has PlayerMovement or PlayerAnchor, the tag is not required.")]
    public string playerTag = "Player";

    private void Awake()
    {
        if (popupUI != null)
            popupUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        if (popupUI != null)
            popupUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other)) return;
        if (popupUI != null)
            popupUI.SetActive(false);
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        // Prefer explicit player components if present in project
        if (col.GetComponentInParent<PlayerMovement>() != null) return true;
        if (col.GetComponentInParent<PlayerAnchor>() != null) return true;
        if (!string.IsNullOrEmpty(playerTag) && col.CompareTag(playerTag)) return true;
        return false;
    }
}
