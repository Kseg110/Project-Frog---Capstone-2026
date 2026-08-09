using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DartFireVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttacks playerAttacks;
    [SerializeField] private PlayerAnchor playerAnchor;

    [Header("Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material fireMaterial;
    [SerializeField] private Material iceMaterial;
    [SerializeField] private Material windMaterial;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color iceColor = Color.cyan;
    [SerializeField] private Color windColor = Color.white;

    [Header("Settings")]
    [SerializeField] private float duration = 0.15f;

    private ParticleSystem fireVFX;
    private ParticleSystemRenderer vfxRenderer;
    private Coroutine vfxCoroutine;

    private void Awake()
    {
        fireVFX = GetComponent<ParticleSystem>();
        vfxRenderer = GetComponent<ParticleSystemRenderer>();

        if (playerAttacks == null)
            playerAttacks = GetComponentInParent<PlayerAttacks>();

        if (playerAnchor == null)
            playerAnchor = GetComponentInParent<PlayerAnchor>();

        fireVFX.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void OnEnable()
    {
        if (playerAttacks != null)
            playerAttacks.OnBasicShotFired += OnBasicShotFired;
    }

    private void OnDisable()
    {
        if (playerAttacks != null)
            playerAttacks.OnBasicShotFired -= OnBasicShotFired;
    }

    private void OnBasicShotFired()
    {
        if (vfxCoroutine != null)
            StopCoroutine(vfxCoroutine);

        vfxCoroutine = StartCoroutine(PlayVFX());
    }

    private System.Collections.IEnumerator PlayVFX()
    {
        fireVFX.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        // Get the correct material and color based
        // on the currently attached anchor.
        Material selectedMaterial;
        Color selectedColor;

        GetVFXSettings(out selectedMaterial, out selectedColor);

        // Apply material.
        if (selectedMaterial != null)
            vfxRenderer.material = selectedMaterial;

        // Apply color.
        var main = fireVFX.main;
        main.startColor = selectedColor;

        // Play the ONE particle system.
        fireVFX.Play();

        yield return new WaitForSeconds(duration);

        fireVFX.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        vfxCoroutine = null;
    }

    private void GetVFXSettings(
        out Material material,
        out Color color)
    {
        // Default values.
        material = normalMaterial;
        color = normalColor;

        // No PlayerAnchor = normal.
        if (playerAnchor == null)
            return;

        // Not attached = normal.
        if (!playerAnchor.IsTethered)
            return;

        // No anchor = normal.
        if (playerAnchor.AttachedAnchor == null)
            return;

        AnchorBase anchor = playerAnchor.AttachedAnchor;

        // Fire anchor.
        if (anchor.BaseData is AnchorFireData)
        {
            material = fireMaterial;
            color = fireColor;
            return;
        }

        // Ice anchor.
        if (anchor.BaseData is AnchorIceData)
        {
            material = iceMaterial;
            color = iceColor;
            return;
        }

        // Wind anchor.
        if (anchor.BaseData is AnchorWindData)
        {
            material = windMaterial;
            color = windColor;
            return;
        }

        // Anything else stays normal.
    }
}