using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDebugCategory", menuName = "Scriptable Objects/PlayerDebugCategory")]
public class PlayerDebugCategory : DebugCategory
{
    [Header("Player references (optional)")]
    [Tooltip("Optional: assign scene references in inspector for convenience. If null, the category will try to FindObjectOfType at runtime.")]
    public CombatStatistics statsAssetReference;   // optional inspector hint only
    public PlayerTakeDamage takeDamageAssetReference;

    [Header("Player tuning ranges")]
    public float minPlayerSpeed = 0f;
    public float maxPlayerSpeed = 20f;
    public float minTongueSpeed = 0f;
    public float maxTongueSpeed = 20f;

    [Header("Defaults")]
    public float defaultPlayerSpeed = 5f;
    public float defaultTongueSpeed = 5f;
    public bool defaultGodMode = false;

    // Runtime-only (do not serialize back to asset)
    [System.NonSerialized] CombatStatistics runtimeStats;
    [System.NonSerialized] PlayerTakeDamage runtimeTakeDamage;
    [System.NonSerialized] float runtimePlayerSpeed;
    [System.NonSerialized] float runtimeTongueSpeed;
    [System.NonSerialized] bool runtimeGodMode;
    [System.NonSerialized] bool runtimeInitialized = false;

    void OnEnable()
    {
        // Initialize runtime values from defaults; scene references resolved later when Draw runs
        runtimePlayerSpeed = defaultPlayerSpeed;
        runtimeTongueSpeed = defaultTongueSpeed;
        runtimeGodMode = defaultGodMode;
        runtimeInitialized = false;
    }

    // Optional: allow external code (such as a DebugMenu MonoBehaviour) to bind scene
    // objects to this category so the ScriptableObject doesn't need to rely on Find in Draw.
    public void BindRuntimeObjects(CombatStatistics stats, PlayerTakeDamage takeDamage)
    {
        if (stats != null) runtimeStats = stats;
        if (takeDamage != null) runtimeTakeDamage = takeDamage;
        // initialize runtime values from bound stats once
        if (!runtimeInitialized && runtimeStats != null)
        {
            runtimePlayerSpeed = runtimeStats.playerSpeed;
            runtimeTongueSpeed = runtimeStats.tongueExtendSpeed;
            runtimeInitialized = true;
        }
    }

    // Called by DebugMenu when this category is active
    public override void Draw()
    {
        GUILayout.Label("Player Debug Options", GUI.skin.box);

        // Ensure we have runtime references (try inspector-assigned first, then Find)
        if (runtimeStats == null) runtimeStats = statsAssetReference;
        if (runtimeTakeDamage == null) runtimeTakeDamage = takeDamageAssetReference;

        // Try to find scene objects every draw in case the scene contains the components
        // but they weren't assigned on the ScriptableObject asset (Unity doesn't serialize
        // scene object references on project assets). Use FindFirstObjectByType which replaces
        // the older FindObjectOfType API.
        if (runtimeStats == null) runtimeStats = Object.FindFirstObjectByType<CombatStatistics>();
        if (runtimeTakeDamage == null) runtimeTakeDamage = Object.FindFirstObjectByType<PlayerTakeDamage>();

        if (runtimeStats == null || runtimeTakeDamage == null)
        {
            GUILayout.Label("Player components not found in scene.", GUI.skin.label);
            GUILayout.BeginHorizontal();    
            if (GUILayout.Button("Try Find Scene Objects", GUILayout.Width(180)))
            {
                runtimeStats = Object.FindFirstObjectByType<CombatStatistics>();
                runtimeTakeDamage = Object.FindFirstObjectByType<PlayerTakeDamage>();
            }
            if (GUILayout.Button("Show Inspector References", GUILayout.Width(180)))
            {
                GUILayout.Label("Assign references on the asset in Project view.", GUI.skin.label);
            }
            GUILayout.EndHorizontal();
            return;
        }

        // Initialize runtime values from the actual components the first time we have them
        if (!runtimeInitialized && runtimeStats != null)
        {
            runtimePlayerSpeed = runtimeStats.playerSpeed;
            runtimeTongueSpeed = runtimeStats.tongueExtendSpeed;
            runtimeInitialized = true;
        }
        if (runtimeTakeDamage != null)
            runtimeGodMode = runtimeTakeDamage.isGod;

        // Player speed control
        GUILayout.BeginHorizontal();
        GUILayout.Label("Player Speed", GUILayout.Width(110));
        float newPlayerSpeed = GUILayout.HorizontalSlider(runtimePlayerSpeed, minPlayerSpeed, maxPlayerSpeed, GUILayout.Width(200));
        string playerSpeedText = GUILayout.TextField(runtimePlayerSpeed.ToString("0.00"), GUILayout.Width(60));
        GUILayout.EndHorizontal();

        if (float.TryParse(playerSpeedText, out float parsedPlayerSpeed))
            newPlayerSpeed = Mathf.Clamp(parsedPlayerSpeed, minPlayerSpeed, maxPlayerSpeed);

        if (!Mathf.Approximately(newPlayerSpeed, runtimePlayerSpeed))
        {
            runtimePlayerSpeed = newPlayerSpeed;
            ApplyPlayerSpeed(runtimePlayerSpeed);
        }

        // Tongue speed control
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tongue Speed", GUILayout.Width(110));
        float newTongueSpeed = GUILayout.HorizontalSlider(runtimeTongueSpeed, minTongueSpeed, maxTongueSpeed, GUILayout.Width(200));
        string tongueSpeedText = GUILayout.TextField(runtimeTongueSpeed.ToString("0.00"), GUILayout.Width(60));
        GUILayout.EndHorizontal();

        if (float.TryParse(tongueSpeedText, out float parsedTongueSpeed))
            newTongueSpeed = Mathf.Clamp(parsedTongueSpeed, minTongueSpeed, maxTongueSpeed);

        if (!Mathf.Approximately(newTongueSpeed, runtimeTongueSpeed))
        {
            runtimeTongueSpeed = newTongueSpeed;
            ApplyTongueSpeed(runtimeTongueSpeed);
        }

        GUILayout.Space(6);

        // God mode toggle
        bool newGod = GUILayout.Toggle(runtimeGodMode, "God Mode (invulnerable)");
        if (newGod != runtimeGodMode)
        {
            runtimeGodMode = newGod;
            ApplyGodMode(runtimeGodMode);
        }

        GUILayout.Space(6);

        // Quick action buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Speeds", GUILayout.Width(140)))
        {
            runtimePlayerSpeed = defaultPlayerSpeed;
            runtimeTongueSpeed = defaultTongueSpeed;
            ApplyPlayerSpeed(runtimePlayerSpeed);
            ApplyTongueSpeed(runtimeTongueSpeed);
        }
        if (GUILayout.Button("Log Current Values", GUILayout.Width(140)))
        {
            Debug.Log($"PlayerSpeed={runtimePlayerSpeed}, TongueSpeed={runtimeTongueSpeed}, God={runtimeGodMode}");
        }
        GUILayout.EndHorizontal();
    }

    void ApplyPlayerSpeed(float speed)
    {
        if (runtimeStats != null)
        {
            runtimeStats.playerSpeed = speed;
        }
    }

    void ApplyTongueSpeed(float speed)
    {
        if (runtimeStats != null)
        {
            runtimeStats.tongueExtendSpeed = speed;
        }
    }

    void ApplyGodMode(bool enabled)
    {
        // If we have a specific bound instance, set it. Also set any remaining instances
        // found in the scene to ensure all player components are toggled.
        if (runtimeTakeDamage != null)
            runtimeTakeDamage.isGod = enabled;

        PlayerTakeDamage[] all = Object.FindObjectsOfType<PlayerTakeDamage>();
        foreach (var p in all)
            p.isGod = enabled;

        Debug.Log("God mode toggled: " + enabled + " (applied to " + all.Length + " PlayerTakeDamage instances)");
    }
}
