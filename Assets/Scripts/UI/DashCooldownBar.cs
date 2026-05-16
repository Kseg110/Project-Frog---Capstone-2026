using UnityEngine;
using UnityEngine.UI;

public class DashCooldownBar : MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    [SerializeField] private Transform uiContainer;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);

    private Camera mainCamera;
    private PlayerMovement playerMovement;
    private Transform playerTransform;

    private void Start()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerTransform = playerMovement.transform;

        // Unparent so it's no longer affected by player rotation
        transform.SetParent(null);
    }

    private void LateUpdate()
    {
        // Follow the player's position with a fixed world-space offset
        if (playerTransform != null)
            transform.position = playerTransform.position + offset;

        // Update fill based on dash cooldown progress
        if (playerMovement != null)
            foregroundImage.fillAmount = playerMovement.DashCooldownProgress;

        // Billboard the UI container toward the camera
        if (uiContainer != null && mainCamera != null)
            uiContainer.forward = mainCamera.transform.forward;
    }
}