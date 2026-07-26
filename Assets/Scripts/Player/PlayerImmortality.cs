using UnityEngine;

public class PlayerImmortality : MonoBehaviour
{
    [Header("Glow Material (assign your material here)")]
    public Material glowMaterial;

    [Header("Glow Settings")]
    public Color glowColor = Color.yellow;
    public float glowIntensity = 5f;

    [SerializeField] private bool isImmortal;
    public bool IsImmortal => isImmortal;

    private Renderer[] renderers;
    private Material[][] originalMaterials; // stores original mats
    private Material glowMaterialInstance;

    private PlayerMovement controller;
    private bool isGlowing = false;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        controller = GetComponent<PlayerMovement>();

        // Save original materials
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
        if (glowMaterial != null)
        {
            glowMaterialInstance = new Material(glowMaterial);
            glowMaterialInstance.EnableKeyword("_EMISSION");
            glowMaterialInstance.SetColor("_EmissionColor", glowColor * glowIntensity);
        }
    }

    private void Update()
    {
        if (controller == null) return;

        isImmortal = controller.IsDashing;

        if (isImmortal && !isGlowing)
            EnableGlow();
        else if (!isImmortal && isGlowing)
            DisableGlow();
    }

    private void EnableGlow()
    {
        if (glowMaterial == null) return;

        // Replace all materials with instanced glow material
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = glowMaterialInstance;

            r.materials = mats;
        }

        isGlowing = true;
    }

    private void DisableGlow()
    {
        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }

        isGlowing = false;
    }

    private void OnDestroy()
    {
        if (glowMaterialInstance != null)
        {
            Destroy(glowMaterialInstance);
        }
    }
}
