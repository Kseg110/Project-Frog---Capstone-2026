using System.Collections;
using UnityEngine;

/// <summary>
/// Simple door behavior to attach to a door/wall parent.
/// - Finds or uses the assigned door Transform and a child trigger collider.
/// - When the player steps on the trigger the door moves down into the ground.
/// - The trigger child will receive a small relay component at Awake if it has no script.
/// </summary>
public class SingleDoor : MonoBehaviour
{
    [Header("Door")]
    [Tooltip("The transform that will move. If null, this GameObject's transform is used.")]
    [SerializeField] private Transform doorTransform;

    [Tooltip("Local offset to move the door when opening (default moves down).")]
    [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, -3f, 0f);

    [Tooltip("Speed in units per second the door moves when opening.")]
    [SerializeField] private float openSpeed = 3f;

    [Header("Trigger")]
    [Tooltip("Optional: assign the trigger GameObject (child). If null the first child trigger collider will be used.")]
    [SerializeField] private GameObject triggerObject;

    [Tooltip("Tag that identifies the player. Default: 'Player'")]
    [SerializeField] private string playerTag = "Player";

    [Header("Options")]
    [Tooltip("If true the door will only open once.")]
    [SerializeField] private bool openOnlyOnce = true;

    [Tooltip("If true the door GameObject will be destroyed after reaching the open position.")]
    [SerializeField] private bool destroyDoorWhenOpened = false;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;
    private bool isOpening = false;
    private bool isOpened = false;

    private void Awake()
    {
        if (doorTransform == null)
            doorTransform = transform;

        closedLocalPos = doorTransform.localPosition;
        openLocalPos = closedLocalPos + openLocalOffset;

        // find trigger if not assigned
        if (triggerObject == null)
        {
            // search children for a collider with isTrigger = true
            foreach (Transform child in transform)
            {
                var col = child.GetComponent<Collider>();
                if (col != null && col.isTrigger)
                {
                    triggerObject = child.gameObject;
                    break;
                }
            }
        }

        if (triggerObject == null)
        {
            Debug.LogWarning($"SingleDoor ({name}): No trigger object found. Please add a child trigger or assign one in the inspector.");
            return;
        }

        // ensure relay is present on the trigger so we get OnTriggerEnter
        var relay = triggerObject.GetComponent<DoorTriggerRelay>();
        if (relay == null)
            relay = triggerObject.AddComponent<DoorTriggerRelay>();

        relay.owner = this;
    }

    internal void OnTriggerActivated(GameObject activator)
    {
        if (isOpened && openOnlyOnce) return;
        if (isOpening) return;

        if (!string.IsNullOrEmpty(playerTag) && !activator.CompareTag(playerTag))
            return;

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isOpening = true;

        while (Vector3.Distance(doorTransform.localPosition, openLocalPos) > 0.01f)
        {
            doorTransform.localPosition = Vector3.MoveTowards(doorTransform.localPosition, openLocalPos, openSpeed * Time.deltaTime);
            yield return null;
        }

        doorTransform.localPosition = openLocalPos;
        isOpening = false;
        isOpened = true;

        if (destroyDoorWhenOpened && doorTransform != null)
        {
            Destroy(doorTransform.gameObject);
        }
    }

    // Relay component attached to the trigger child so the parent SingleDoor gets notified.
    private class DoorTriggerRelay : MonoBehaviour
    {
        public SingleDoor owner;

        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            owner.OnTriggerActivated(other.gameObject);
        }
    }
}
