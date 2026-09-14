using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using FMODUnity;

// Drives the fade out system which calls Die() from Health.cs in order to play both cleanly fade Enemies from the scene, and - if applicable - play a death animation. -E.M
public class EnemyFadeOut : MonoBehaviour
{
    // How held weapons are handled when the enemy dies. Two choices available;
    public enum WeaponDeathMode
    {
        FadeWithBody,        // Weapon renderers are folded into the body fade and dissolve together.
        DisableImmediately   // Weapon is switched off the moment Die() runs.
    }

    [Header("Death Rig Swap (optional)")]
    [Tooltip("Optional. Mesh/rig to DISABLE on death (e.g. the live animated TPose rig). Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private GameObject meshToDisable;
    [Tooltip("Optional. Mesh/rig to ENABLE on death (e.g. the fall-apart corpse rig). Should share the same parent transform as the disabled mesh so it appears in place. Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private GameObject meshToEnable;
    [Tooltip("Optional. Animator whose Avatar is cleared on death (e.g. the live rig's Animator). Cleared only at death so the live rig's animations still play normally while alive. Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private Animator avatarToClear;

    [Header("Death Animation")]
    [Tooltip("Left empty = auto-filled on Awake (from the swap-in mesh if one is assigned, otherwise from this object).")]
    [SerializeField] private Animator animator;
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    [Header("Disable On Death")]
    [Tooltip("Behaviour scripts to switch off on death (AI, movement, attack) to prevent phantom movement. Left empty = auto-filled from EnemyBase.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [Tooltip("Halted on death so residual velocity / last destination doesn't keep the enemy drifting. Left empty = auto-filled from this object.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Held Weapons")]
    [Tooltip("Weapons parented to bones (e.g. the SpearPrefab under RightHand). These sit outside the main renderer array, so list them here.")]
    [SerializeField] private GameObject[] weaponObjects;
    [Tooltip("FadeWithBody dissolves the weapon alongside the corpse. DisableImmediately just switches it off at the moment of death.")]
    [SerializeField] private WeaponDeathMode weaponDeathMode = WeaponDeathMode.FadeWithBody;
    [Tooltip("Weapons already hidden when the enemy dies (e.g. a spear mid-flight) stay hidden rather than popping back into the hand.")]
    [SerializeField] private bool ignoreAlreadyHiddenWeapons = true;

    [Header("Fade")]
    [SerializeField] private Material deathMaterial;
    [SerializeField] private float duration = 1.0f;
    [Tooltip("Left empty = auto-filled on Awake (from the swap-in mesh if one is assigned, otherwise from all child renderers).")]
    [SerializeField] private Renderer[] renderers;
    [Tooltip("Forces depth writing on the transparent death material so the far side of the mesh doesn't show through the near side.")]
    [SerializeField] private bool forceDepthWriteOnFade = true;
    [Tooltip("Stops the fading corpse casting a full-strength shadow after it has visually disappeared.")]
    [SerializeField] private bool disableShadowsOnFade = true;

    [Header("Fade Property Resolution")]
    [Tooltip("Exposed alpha property on the death material's shader - use the Reference name from Shader Graph's Node/Property settings, e.g. _Alpha. Leave empty to auto-detect (_Alpha, _Opacity, then _BaseColor / _Color alpha).")]
    [SerializeField] private string alphaPropertyOverride = "";
    [Tooltip("Exposed emission property to dim alongside alpha, again by Reference name (e.g. _EmissionStrength). Leave empty to auto-detect (_EmissionStrength, then _EmissionColor). Shaders with neither simply skip the emission ramp.")]
    [SerializeField] private string emissionPropertyOverride = "";

    [Header("Disable On Fade")]
    [Tooltip("Left empty = auto-filled from all child colliders on Awake.")]
    [SerializeField] private Collider[] collidersToDisable;

    [Header("Health Bar Disable")]
    [Tooltip("If the Enemy currently fading has a health bar - disable that shizzle homeboy!")]
    [SerializeField] private GameObject healthBar;

    [Header("FMod Events")]
    [SerializeField] private EventReference enemyDeathEvent;

    // Checked in order, and only the FIRST match on a given shader is used.
    // Scalar Shader Graph properties come first: a graph that exposes its own _Alpha has it wired to the Fragment stage's Alpha block, whereas a _BaseColor that URP injects for material override may not drive anything.
    // Stock URP Lit has no _Alpha, so it still falls through to _BaseColor exactly as before.
    private static readonly string[] DefaultAlphaCandidates = { "_Alpha", "_Opacity", "_BaseColor", "_Color" };
    private static readonly string[] DefaultEmissionCandidates = { "_EmissionStrength", "_EmissionColor" };

    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int ZWriteControlId = Shader.PropertyToID("_ZWriteControl");   // Shader Graph's Depth Write dropdown: 0 = Auto, 1 = ForceEnabled, 2 = ForceDisabled

    // One entry per material slot per renderer, resolved once when the fade starts so the per-frame ramp is pure Set calls - no HasProperty / string lookups, and no re-reading an already dimmed emission value.
    private struct FadeTarget
    {
        public Material material;
        public bool hasAlpha;
        public int alphaId;
        public bool alphaIsScalar;      // true = SetFloat (Shader Graph slider), false = alpha channel of a Color
        public bool hasEmission;
        public int emissionId;
        public bool emissionIsScalar;
        public Color emissionColor;     // captured start value, so we scale from the true original
        public float emissionScalar;
    }

    private readonly List<FadeTarget> fadeTargets = new List<FadeTarget>();
    private bool isFading;
    private bool isDead;   // guard so Die() only runs once

    private void Awake()
    {
        // If a swap-in mesh is assigned, target its animator/renderers (it may be inactive at Awake, so include inactive).
        // Otherwise fall back to this object, preserving original behaviour for enemies that don't swap rigs.
        if (animator == null)
            animator = meshToEnable != null
                ? meshToEnable.GetComponentInChildren<Animator>(true)
                : GetComponent<Animator>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (renderers == null || renderers.Length == 0)
            renderers = meshToEnable != null
                ? meshToEnable.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>();
        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();
        if (scriptsToDisable == null || scriptsToDisable.Length == 0)
        {
            var b = GetComponent<EnemyBase>();
            if (b != null) scriptsToDisable = new MonoBehaviour[] { b };
        }
    }

    // Call this from the health system when HP hits 0.
    // Stops AI + agent + colliders, plays the baked "fall apart" animation (or any other animation that calls isDead), waits for it to finish, then fades out and destroys.
    public void Die()
    {
        if (isDead) return;   // guard against double-death
        isDead = true;

        RuntimeManager.PlayOneShot(enemyDeathEvent, transform.position);

        // Disables the Enemy's health bar as soon as the Enemy's health reaches 0.
        if (healthBar != null)
            healthBar.SetActive(false);

        // Snapshot which weapons were visible BEFORE the AI scripts are switched off - disabling a component fires its OnDisable, which may re-show a thrown weapon.
        CacheVisibleWeapons();

        // Stop the AI from steering a dying enemy (kills the per-frame MoveToTarget at the source).
        foreach (var s in scriptsToDisable)
            if (s != null) s.enabled = false;

        // Halt the NavMeshAgent so residual velocity / last destination doesn't keep the enemy drifting.
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        ApplyWeaponDeathState();

        // Done BEFORE the isDead trigger so the death animation fires on the swapped-in mesh, not the live one.
        // Both meshes should share the same parent transform, so the swap-in appears in the same position/rotation.
        // Skipped entirely when the fields are left empty (enemies that don't swap rigs are unaffected).

        // Clear the live rig's Avatar at death (not before) so its animations run normally while alive, but stop driving the shared skeleton once we hand off to the corpse rig.
        if (avatarToClear != null)
            avatarToClear.avatar = null;

        if (meshToDisable != null)
            meshToDisable.SetActive(false);
        if (meshToEnable != null)
            meshToEnable.SetActive(true);

        // Disable colliders immediately so a dying enemy stops blocking movement / registering on the tether.
        foreach (var c in collidersToDisable)
            if (c != null) c.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger(IsDeadHash);
            StartCoroutine(DeathSequence());
        }
        else
        {
            // No animator wired - skip straight to the fade.
            BeginFade();
        }
    }

    // Weapons that were visible at the moment of death. Anything hidden at that point (a spear already thrown) is left out so it doesn't reappear on the corpse.
    private readonly List<GameObject> weaponsAtDeath = new List<GameObject>();

    private void CacheVisibleWeapons()
    {
        weaponsAtDeath.Clear();
        if (weaponObjects == null) return;

        foreach (var w in weaponObjects)
        {
            if (w == null) continue;
            if (ignoreAlreadyHiddenWeapons && !w.activeInHierarchy) continue;
            weaponsAtDeath.Add(w);
        }
    }

    private void ApplyWeaponDeathState()
    {
        if (weaponDeathMode != WeaponDeathMode.DisableImmediately) return;

        foreach (var w in weaponsAtDeath)
            if (w != null) w.SetActive(false);

        weaponsAtDeath.Clear();
    }

    // Folds the weapon's renderers into the main array so they go through the exact same material swap, alpha ramp and cleanup as the body. Called just before the fade starts.
    private void MergeWeaponRenderers()
    {
        if (weaponDeathMode != WeaponDeathMode.FadeWithBody) return;
        if (weaponsAtDeath.Count == 0) return;

        var merged = new List<Renderer>(renderers ?? new Renderer[0]);

        foreach (var w in weaponsAtDeath)
        {
            if (w == null) continue;

            // Re-show the weapon if the rig swap or an OnDisable turned it off between death and fade - it was visible when the enemy died, so it should dissolve.
            if (!w.activeSelf) w.SetActive(true);

            foreach (var r in w.GetComponentsInChildren<Renderer>(true))
                if (r != null && !merged.Contains(r))
                    merged.Add(r);
        }

        renderers = merged.ToArray();
    }

    private IEnumerator DeathSequence()
    {
        // Wait for the transition into the death state to begin AND complete - while a transition is running, GetCurrentAnimatorStateInfo still reports the OUTGOING state, so reading length too early returns the locomotion clip's length.
        yield return null;
        while (animator.IsInTransition(0))
            yield return null;

        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);
        BeginFade();
    }

    public void BeginFade()
    {
        if (isFading) return;   // guard against double-trigger
        isFading = true;

        // Pull held weapons into the renderer array first so they share the whole pipeline.
        MergeWeaponRenderers();

        // Swap to the transparent death material FIRST - the fade targets are resolved against the instanced copies of THAT material, so doing this after would cache properties from the wrong shader.
        ChangeToDeathMaterial();
        BuildFadeTargets();
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    // Resolves the alpha (and optional emission) property once per instanced material, using the shader's own property table rather than assuming _BaseColor.
    // Shader Graph exposes alpha as a standalone float/slider rather than the alpha channel of a color, so we check for both. If no alpha property is found, we log a warning so the designer knows the death material would pop instead of fading.
    private void BuildFadeTargets()
    {
        fadeTargets.Clear();
        bool warnedAboutAlpha = false;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            // r.materials returns the per-renderer instances created in ChangeToDeathMaterial, so edits here are local to this corpse.
            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                var target = new FadeTarget { material = mat };

                target.hasAlpha = TryResolveAlpha(mat, out int alphaId, out bool alphaIsScalar);
                target.alphaId = alphaId;
                target.alphaIsScalar = alphaIsScalar;

                target.hasEmission = TryResolveEmission(mat, out int emissionId, out bool emissionIsScalar);
                target.emissionId = emissionId;
                target.emissionIsScalar = emissionIsScalar;
                if (target.hasEmission)
                {
                    if (emissionIsScalar) target.emissionScalar = mat.GetFloat(emissionId);
                    else target.emissionColor = mat.GetColor(emissionId);
                }

                if (!target.hasAlpha && !warnedAboutAlpha)
                {
                    warnedAboutAlpha = true;
                    Debug.LogWarning($"[EnemyFadeOut] Shader '{mat.shader.name}' on {gameObject.name} exposes no alpha property this script recognises, so the corpse will pop out instead of fading. Set Alpha Property Override to the shader's Reference name (Shader Graph: select the property, copy the Reference field, e.g. _Alpha).");
                }

                fadeTargets.Add(target);
            }
        }
    }

    private bool TryResolveAlpha(Material mat, out int id, out bool isScalar)
    {
        if (TryResolveProperty(mat, alphaPropertyOverride, out id, out isScalar)) return true;

        foreach (var name in DefaultAlphaCandidates)
            if (TryResolveProperty(mat, name, out id, out isScalar)) return true;

        id = 0;
        isScalar = false;
        return false;
    }

    private bool TryResolveEmission(Material mat, out int id, out bool isScalar)
    {
        if (TryResolveProperty(mat, emissionPropertyOverride, out id, out isScalar)) return true;

        foreach (var name in DefaultEmissionCandidates)
            if (TryResolveProperty(mat, name, out id, out isScalar)) return true;

        id = 0;
        isScalar = false;
        return false;
    }

    // Material.HasProperty is type-blind - it returns true for a texture named _Emission just as happily as for a float - so we query the shader's property table instead and record whether to drive it as a float or a colour.
    private static bool TryResolveProperty(Material mat, string propertyName, out int id, out bool isScalar)
    {
        id = 0;
        isScalar = false;

        if (mat == null || string.IsNullOrEmpty(propertyName)) return false;

        Shader shader = mat.shader;
        if (shader == null) return false;

        int index = shader.FindPropertyIndex(propertyName);
        if (index < 0) return false;

        switch (shader.GetPropertyType(index))
        {
            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                isScalar = true;
                break;
            case ShaderPropertyType.Color:
            case ShaderPropertyType.Vector:
                isScalar = false;
                break;
            default:
                return false;   // Texture / Int - nothing we can ramp.
        }

        id = Shader.PropertyToID(propertyName);
        return true;
    }

    private IEnumerator FadeRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - t / duration);
            ApplyAlpha(alpha);
            yield return null;
        }
        ApplyAlpha(0f);

        // Safety net: alpha and emission are both zeroed, but hard-disable renderers so any residual specular/reflection is gone before destroy.
        foreach (var r in renderers)
            if (r != null) r.enabled = false;
        Destroy(gameObject);
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < fadeTargets.Count; i++)
        {
            var target = fadeTargets[i];
            var mat = target.material;
            if (mat == null) continue;

            if (target.hasAlpha)
            {
                if (target.alphaIsScalar)
                {
                    mat.SetFloat(target.alphaId, alpha);
                }
                else
                {
                    Color c = mat.GetColor(target.alphaId);
                    c.a = alpha;
                    mat.SetColor(target.alphaId, c);
                }
            }

            // Dim emission by the same factor so the glow dies with the surface.
            if (target.hasEmission)
            {
                if (target.emissionIsScalar) mat.SetFloat(target.emissionId, target.emissionScalar * alpha);
                else mat.SetColor(target.emissionId, target.emissionColor * alpha);
            }
        }
    }

    // Replaces EVERY material slot on every tracked renderer with the transparent death material. The single-slot version only swapped submesh 0, leaving the rest opaque.
    private void ChangeToDeathMaterial()
    {
        if (deathMaterial == null)
        {
            Debug.LogError($"[EnemyFadeOut] No deathMaterial assigned on {gameObject.name}. The enemy will pop out instead of fading.");
            return;
        }

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = deathMaterial;
            r.materials = mats;   // assigning the array instantiates per-renderer copies

            if (forceDepthWriteOnFade)
            {
                // Transparent materials disable ZWrite, which lets the far side of the mesh render through the near side (the "x-ray" look).
                // Shader Graph gates this behind _ZWriteControl (its Depth Write dropdown, "Auto" by default), so set both:
                // the control tells the shader to honour the override, _ZWrite is the value the pass actually reads.
                var instanced = r.materials;
                for (int i = 0; i < instanced.Length; i++)
                {
                    var mat = instanced[i];
                    if (mat == null) continue;
                    if (mat.HasProperty(ZWriteControlId)) mat.SetFloat(ZWriteControlId, 1f);   // ForceEnabled
                    if (mat.HasProperty(ZWriteId)) mat.SetFloat(ZWriteId, 1f);
                }
            }

            if (disableShadowsOnFade)
                r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}