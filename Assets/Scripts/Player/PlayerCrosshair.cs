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

    private Canvas uiCanvas;
    private Image crosshairImage;
    private RectTransform crosshairRect;
    private Transform player;
    private Camera cam;

    private bool usingController = false;
    private Vector2 controllerLookInput;

    void Awake()
    {
        // Ensure cursor is not locked by other systems
        Cursor.lockState = CursorLockMode.None;
        cam = Camera.main;

        GameObject p =
        GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;

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
    }

    public Vector3 GetLookDirection()
    {
        Vector3 dir =
        new Vector3(
            controllerLookInput.x, 0, controllerLookInput.y);
        return dir.normalized;
    }

    void LateUpdate()
    {
        if (crosshairRect == null)
            return;

        if (usingController)
        {
            if (player == null)
                return;

            Vector3 playerScreenPos = cam.WorldToScreenPoint(player.position);

            Vector2 stickDir;

            if (controllerLookInput.sqrMagnitude > 0.01f)
            {
                stickDir = controllerLookInput.normalized;
            }
            else
            {
                // keep last direction when stick released
                stickDir = (Vector2)(crosshairRect.position - playerScreenPos);

                if (stickDir.sqrMagnitude > 0.01f)
                {
                    stickDir.Normalize();
                }
                else
                {
                    // default start direction
                    stickDir = Vector2.up;
                }
            }

            Vector2 offset = stickDir * controllerRadius;
            crosshairRect.position = playerScreenPos + (Vector3)offset;
        }
        else
        {
            crosshairRect.position = Input.mousePosition;
        }

        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
    }

    void SetupCanvasAndCrosshair()
    {
        // Try to find an existing overlay canvas first
        uiCanvas = Object.FindFirstObjectByType<Canvas>();
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
