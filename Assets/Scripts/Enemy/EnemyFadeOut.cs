using System.Collections;
using UnityEngine;
using UnityEngine.AI;
// Drives the fade out system which calls Die() from Health.cs in order to play both cleanly fade Enemies from the scene, and - if applicable - play a death animation. -E.M
public class EnemyFadeOut : MonoBehaviour
{
    [Header("Death Animation")]
    [Tooltip("Left empty = auto-filled from this object on Awake.")]
    [SerializeField] private Animator animator;
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    [Header("Disable On Death")]
    [Tooltip("Behaviour scripts to switch off on death (AI, movement, attack) to prevent phantom movement. Left empty = auto-filled from EnemyBase.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [Tooltip("Halted on death so residual velocity / last destination doesn't keep the enemy drifting. Left empty = auto-filled from this object.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Fade")]
    [SerializeField] private Material deathMaterial;
    [SerializeField] private float duration = 1.0f;
    [Tooltip("Left empty = auto-filled from all child renderers on Awake.")]
    [SerializeField] private Renderer[] renderers;

    [Header("Disable On Fade")]
    [Tooltip("Left empty = auto-filled from all child colliders on Awake.")]
    [SerializeField] private Collider[] collidersToDisable;

    [Header("Health Bar Disable")]
    [Tooltip("If the Enemy currently fading has a health bar - disable that shizzle homeboy!")]
    [SerializeField] private GameObject healthBar;

    // URP Lit uses _BaseColor; some shaders (or Built-in) use _Color. Resolve per-material.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    // Original emission colors, captured at fade start so we scale from the true value rather than compounding frame to frame.
    private Color[] baseEmission;
    private bool isFading;
    private bool isDead;   // guard so Die() only runs once
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();
        if (scriptsToDisable == null || scriptsToDisable.Length == 0)
        {
            var b = GetComponent<EnemyBase>();
            if (b != null) scriptsToDisable = new MonoBehaviour[] { b };
        }
    }
    // Call this from the health system when HP hits 0.
    // Stops AI + agent + colliders, plays the baked "fall apart" animation, waits for it to finish, then fades out and destroys.
    public void Die()
    {
        if (isDead) return;   // guard against double-death
        isDead = true;

        // Disables the Enemy's health bar as soon as the Enemy's health reaches 0.
        if (healthBar != null)
            healthBar.SetActive(false);

        // Stop the AI from steering a dying frog (kills the per-frame MoveToTarget at the source).
        foreach (var s in scriptsToDisable)
            if (s != null) s.enabled = false;

        // Halt the NavMeshAgent so residual velocity / last destination doesn't keep the frog drifting.
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

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
            // No animator wired — skip straight to the fade.

            ChangeToDeathMaterial();
            BeginFade();
        }
    }
    private IEnumerator DeathSequence()
    {
        // Wait one frame so the transition into the death state actually begins, then read the state's length and wait it out.
        yield return null;
        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);
        BeginFade();
    }
    public void BeginFade()
    {
        if (isFading) return;   // guard against double-trigger
        isFading = true;

        CaptureEmission();
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }
    // Snapshot each renderer's starting emission color once, so ApplyAlpha can scale from the original toward black instead of reading an already-dimmed value each frame.
    private void CaptureEmission()
    {
        baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null && r.material.HasProperty(EmissionColorId))
                baseEmission[i] = r.material.GetColor(EmissionColorId);
        }
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
        // Safety net: base-color alpha and emission are both zeroed, but hard-disable renderers so any residual specular/reflection is gone before destroy.
        foreach (var r in renderers)
            if (r != null) r.enabled = false;
        Destroy(gameObject);
    }
    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            // Fade base color alpha.
            int propId;
            if (r.material.HasProperty(BaseColorId)) propId = BaseColorId;
            else if (r.material.HasProperty(ColorId)) propId = ColorId;
            else propId = 0;
            if (propId != 0)
            {
                Color c = r.material.GetColor(propId);
                c.a = alpha;
                r.material.SetColor(propId, c);
            }
            // Fade emission toward black by the same factor so the glow dies with the surface.
            if (baseEmission != null && r.material.HasProperty(EmissionColorId))
                r.material.SetColor(EmissionColorId, baseEmission[i] * alpha);
        }
    }


    private void ChangeToDeathMaterial()
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.material = deathMaterial;
            Debug.Log($"Changed material of {renderer.gameObject.name} to {deathMaterial.name}.");
        }
    }
}
