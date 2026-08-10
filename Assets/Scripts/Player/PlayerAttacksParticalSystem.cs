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

    [Header("Particle Size")]
    [SerializeField] private float normalSize = 1f;
    [SerializeField] private float overchargeSize = 2f;

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
            playerAttacks.OnShotFired += OnShotFired;
    }

    private void OnDisable()
    {
        if (playerAttacks != null)
            playerAttacks.OnShotFired -= OnShotFired;
    }

    // false = primary attack
    // true = secondary attack
    private void OnShotFired(bool isSecondaryAttack)
    {
        if (vfxCoroutine != null)
            StopCoroutine(vfxCoroutine);

        vfxCoroutine = StartCoroutine(
            PlayVFX(isSecondaryAttack)
        );
    }

    private System.Collections.IEnumerator PlayVFX(bool isSecondaryAttack)
    {
        fireVFX.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        // Get material and color based on anchor.
        Material selectedMaterial;
        Color selectedColor;

        GetVFXSettings(
            out selectedMaterial,
            out selectedColor
        );

        if (selectedMaterial != null)
            vfxRenderer.material = selectedMaterial;

        var main = fireVFX.main;
        main.startColor = selectedColor;

        // -----------------------------------------
        // PARTICLE SIZE
        // -----------------------------------------

        if (isSecondaryAttack)
        {
            // Secondary attack = BIG particle
            main.startSize = overchargeSize;
        }
        else
        {
            // Primary attack = NORMAL particle
            main.startSize = normalSize;
        }

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

        if (playerAnchor == null)
            return;

        if (!playerAnchor.IsTethered)
            return;

        if (playerAnchor.AttachedAnchor == null)
            return;

        AnchorBase anchor = playerAnchor.AttachedAnchor;

        // Fire
        if (anchor.BaseData is AnchorFireData)
        {
            material = fireMaterial;
            color = fireColor;
            return;
        }

        // Ice
        if (anchor.BaseData is AnchorIceData)
        {
            material = iceMaterial;
            color = iceColor;
            return;
        }

        // Wind
        if (anchor.BaseData is AnchorWindData)
        {
            material = windMaterial;
            color = windColor;
            return;
        }
    }
}