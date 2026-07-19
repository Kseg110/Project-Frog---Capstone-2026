using UnityEngine;
using UnityEngine.UI;

//Nick: Make a gameobject called crosshairManager and attach this script to it.
//It will create a crosshair image that follows the mouse cursor. You can assign a custom sprite or let it generate a simple one for you.
//It also hides the system cursor while playing if you choose to do so.

public class PlayerCrosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Tooltip("Optional sprite to use for the crosshair. If null, a temporary crosshair will be generated.")]
    [SerializeField] private Sprite crosshairSprite;

    [Tooltip("Size of the crosshair in pixels.")]
    [SerializeField] private Vector2 size = new Vector2(32f, 32f);

    [Tooltip("If true, hides the system cursor while playing.")]
    [SerializeField] private bool hideSystemCursor = true;

    [Header("Controller Mode")]
    [SerializeField] private float controllerRadius = 200f;

    [Header("World Position Settings")]
    [Tooltip("Layer mask for the ground to raycast against")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Tooltip("Visualizer for world target position with debug object")]
    [SerializeField] private GameObject worldTargetIndicator;

    [Tooltip("Constrain Y height for target position")]
    [SerializeField] private float targetYHeight = 0f;

    [Tooltip("True forces target position to stay at targetYHeight")]
    [SerializeField] private bool constrainToGroundLevel = true;

    [Header("Visual Alignment")]
    [Tooltip("Moves UI crosshair to match where projectiles will hit")]
    [SerializeField] private bool alignCrosshairToWorldTarget = true;
    [SerializeField] private float CrosshairSmoothSpeed = 15f;


    private Canvas uiCanvas;
    private Image crosshairImage;
    private RectTransform crosshairRect;
    private Transform player;
    private Camera cam;

    private bool usingController = false;
    private Vector2 initialControllerDirection = Vector2.up;
    private Vector2 controllerLookInput;
    private Vector2 lastControllerDirection;
    private Vector3 worldTargetPosition;
    private bool hasValidWorldTarget = false;


    void Awake()
    {
        // Ensure cursor is not locked by other systems
        Cursor.lockState = CursorLockMode.None;
        if (worldTargetIndicator != null)
            worldTargetIndicator.SetActive(true);
        cam = Camera.main;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        
        if (p != null)
            player = p.transform;

        lastControllerDirection = initialControllerDirection.normalized;

        SetupCanvasAndCrosshair();
    }

    void OnEnable()
    {
        // Apply visibility according to setting when this script becomes active
        Cursor.visible = !hideSystemCursor;
    }

    void OnDisable()
    {
        // Restore cursor visibility when disabled (avoid leaving cursor hidden in editor)
        Cursor.visible = true;
    }

    public void SetControllerMode(bool active)
    {
        usingController = active;
    }

    public void UpdateControllerLook(Vector2 lookInput)
    {
        controllerLookInput = lookInput;
        if (lookInput.sqrMagnitude > 0.01f)
        {
            lastControllerDirection = lookInput.normalized;   
        }
    }

    public Vector3 GetLookDirection()
    {
        // return direction from player to world target position
        if (hasValidWorldTarget && player != null)
        {
            Vector3 dir = worldTargetPosition - player.position;
            dir.y = 0;
            return dir.normalized;
        }
        // controller fallback
        Vector3 fallbackDir = new Vector3(controllerLookInput.x, 0, controllerLookInput.y);
        return fallbackDir.normalized;
    }
    
    // Get world position where the crosshair is actually pointing on the ground
    public Vector3 GetWorldTargetPosition()
    {
        return worldTargetPosition;
    }

    // Returns true if there is a valid world target position
    public bool HasValidWorldTarget()
    {
        return hasValidWorldTarget;
    }

    void LateUpdate()
    {
        if (crosshairRect == null)
            return;
        Vector3 inputScreenPos;

        if (usingController)
        {
            if (player == null)
                return;

            Vector3 playerScreenPos = cam.WorldToScreenPoint(player.position);
            Vector2 stickDir = lastControllerDirection;
            Vector2 offset = stickDir * controllerRadius;
            inputScreenPos = playerScreenPos + (Vector3)offset;
        }
        else
        {
            inputScreenPos = Input.mousePosition;
        }
        //Raycast from camera through crosshair position to find world position
        UpdateWorldTargetPosition(inputScreenPos);

        // Position crosshair based on alignment settings
        if (alignCrosshairToWorldTarget && hasValidWorldTarget)
        {
            // Converts world target to screenspace
            Vector3 targetScreenPos = cam.WorldToScreenPoint(worldTargetPosition);

            if (CrosshairSmoothSpeed > 0f)
            {
                crosshairRect.position = Vector3.Lerp(
                    crosshairRect.position,
                    targetScreenPos,
                    Time.deltaTime * CrosshairSmoothSpeed
                    );
            }
            else
            {
                crosshairRect.position = targetScreenPos;
            }
        }
        else
        {
            crosshairRect.position = inputScreenPos;
        }

        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
    }

    void UpdateWorldTargetPosition(Vector3 screenPosition)
    {
        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            worldTargetPosition = hit.point;
            if (constrainToGroundLevel)
            {
                worldTargetPosition.y = targetYHeight;
            }
            hasValidWorldTarget = true;
            // Update debug indicator
            if (worldTargetIndicator != null)
            {
                if (!worldTargetIndicator.activeSelf)
                {
                    Debug.Log("[PlayerCrosshair] Re-activating indicator");
                    worldTargetIndicator.SetActive(true);
                }
                worldTargetIndicator.transform.position = worldTargetPosition;
            }
        }
        else
        {
            hasValidWorldTarget = false;
            if (worldTargetIndicator != null && worldTargetIndicator.activeSelf)
            {
                Debug.Log("[PlayerCrosshair] Deactivating indicator - no raycast hit");
                worldTargetIndicator.SetActive(false);
            }
        }
    }

    void SetupCanvasAndCrosshair()
    {
        // Try to find an existing overlay canvas first
        //uiCanvas = Object.FindFirstObjectByType<Canvas>();
        if (uiCanvas == null || uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            var canvasGo = new GameObject("CrosshairCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = canvasGo.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Create crosshair image object
        var imgGo = new GameObject("CrosshairImage", typeof(Image));
        imgGo.transform.SetParent(uiCanvas.transform, false);

        crosshairImage = imgGo.GetComponent<Image>();
        crosshairRect = imgGo.GetComponent<RectTransform>();
        crosshairRect.sizeDelta = size;
        crosshairImage.raycastTarget = false;

        // Use provided sprite or generate a temporary one
        if (crosshairSprite != null)
        {
            crosshairImage.sprite = crosshairSprite;
            crosshairImage.preserveAspect = true;
        }
        else
        {
            crosshairImage.sprite = GenerateTemporaryCrosshairSprite((int)size.x, (int)size.y);
        }

        // Start centered
        crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.position = Input.mousePosition;
    }

    // Generates a simple temporary crosshair sprite (transparent background with a 1px cross).
    Sprite GenerateTemporaryCrosshairSprite(int width, int height)
    {
        if (width < 8) width = 8;
        if (height < 8) height = 8;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        // Fill transparent
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

        // Draw central vertical and horizontal lines (1px)
        int cx = width / 2;
        int cy = height / 2;

        for (int y = 0; y < height; y++)
            pixels[y * width + cx] = white;

        for (int x = 0; x < width; x++)
            pixels[cy * width + x] = white;

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
