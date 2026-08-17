using UnityEngine;

/* Allows some Capsule Colliders to hijack the AnchorTether's particles - acting as a "hitbox rider" that moves along the rope and can trigger gameplay effects (damage, knockback, switches).
   Each hitbox carries one particle effect per element (Fire/Ice/Wind); only the effect matching the currently-attached anchor is active. -E.M */
[RequireComponent(typeof(AnchorTether))]
public class TetherHitboxRider : MonoBehaviour
{
    [SerializeField] private AnchorTether tether;
    [SerializeField] private GameObject hitboxPrefab;

    [Header("Element VFX (one per anchor type)")]
    [SerializeField] private ParticleSystem fireVFX;
    [SerializeField] private ParticleSystem iceVFX;
    [SerializeField] private ParticleSystem windVFX;

    [Header("Fade-Out On Detach")]
    [Tooltip("When the tether detaches/breaks, spawn one detached copy of the last-played element VFX at each hitbox's last position so the effect lingers and fades instead of cutting instantly.")]
    [SerializeField] private bool spawnFadeOutOnDetach = true;
    [Tooltip("Safety cap: the fade-out instances self-destruct after this many seconds even if their ParticleSystem reports it's still alive.")]
    [SerializeField] private float fadeOutMaxLifetime = 3f;

    [Range(2, 20)][SerializeField] private int hitboxCount = 8;
    [Tooltip("Only activate hitboxes while attached to an anchor (uses OnAnchorAttached/Detached).")]
    [SerializeField] private bool onlyWhileAttached = true;

    // Holds the three element effects spawned on a single hitbox so we can toggle between them.
    private struct HitboxEffects
    {
        public ParticleSystem fire;
        public ParticleSystem ice;
        public ParticleSystem wind;
    }

    private Rigidbody[] bodies;
    private Transform[] hitboxes;
    private HitboxEffects[] hitboxEffects;
    private bool active;

    // The element currently being shown, cached so HandleDetached knows which VFX to spawn as a fade-out (by the time detach fires, the anchor is already gone so it can't read it from the tether).
    private AnchorElement? currentElement;

    private void Awake()
    {
        if (tether == null) tether = GetComponent<AnchorTether>();
    }

    private void OnEnable()
    {
        tether.OnAnchorAttached += HandleAttached;
        tether.OnAnchorDetached += HandleDetached;
    }

    private void OnDisable()
    {
        tether.OnAnchorAttached -= HandleAttached;
        tether.OnAnchorDetached -= HandleDetached;
    }

    private void Start()
    {
        bodies = new Rigidbody[hitboxCount];
        hitboxes = new Transform[hitboxCount];
        hitboxEffects = new HitboxEffects[hitboxCount];

        // If the tether attached BEFORE this Start ran (scene-load attach, or another script's earlier Start calling SetEndPoint), don't stomp that state - ask the tether directly instead of assuming detached.
        active = !onlyWhileAttached || tether.CurrentAnchor != null;

        for (int i = 0; i < hitboxCount; i++)
        {
            GameObject go = Instantiate(hitboxPrefab, transform);
            hitboxes[i] = go.transform;
            bodies[i] = go.GetComponent<Rigidbody>();

            // Spawn all three element effects parented to this hitbox. They ride the rope automatically because the hitbox itself is moved each FixedUpdate. Only one is enabled at a time.
            hitboxEffects[i] = new HitboxEffects
            {
                fire = SpawnEffect(fireVFX, go.transform),
                ice = SpawnEffect(iceVFX, go.transform),
                wind = SpawnEffect(windVFX, go.transform)
            };

            go.SetActive(active);
        }

        // If we started already attached, show the correct element straight away.
        if (active && tether.CurrentAnchor != null)
            ApplyElement(tether.CurrentAnchor.Element);
        else
            ApplyElement(null);
    }

    // Instantiates a single effect as a child of the hitbox, snapped to local origin. Starts disabled; ApplyElement decides which one turns on. Returns null (and warns) if the prefab wasn't assigned.
    private ParticleSystem SpawnEffect(ParticleSystem prefab, Transform parent)
    {
        //if (prefab == null)
        //{
        //    //Debug.LogWarning($"[TetherHitboxRider] An element VFX prefab is not assigned on {name} - that element will show no effect.");
        //    return null;
        //}

        ParticleSystem fx = Instantiate(prefab, parent);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        fx.gameObject.SetActive(false); // stay off until an anchor of this element attaches
        return fx;
    }

    private void HandleAttached(AnchorBase anchor)
    {
        SetHitboxesActive(true);
        ApplyElement(anchor.Element);
    }

    private void HandleDetached()
    {
        // Spawn the lingering fade-out copies at each hitbox's last position BEFORE we disable them, so the last-played element dissipates in place instead of cutting instantly.
        if (spawnFadeOutOnDetach)
            SpawnFadeOutBurst();

        SetHitboxesActive(!onlyWhileAttached ? active : false);
        ApplyElement(null); // turn all element effects off while detached
    }

    // Spawns one detached copy of the last played element's VFX at each hitbox's current world position/rotation. Unparented so it survives the hitbox being disabled this same frame, and set to self-destruct once it finishes (or after fadeOutMaxLifetime as a hard cap).
    private void SpawnFadeOutBurst()
    {
        ParticleSystem prefab = PrefabForElement(currentElement);
        if (prefab == null) return;          // no element was showing, or that slot is unassigned
        if (hitboxes == null) return;

        for (int i = 0; i < hitboxCount; i++)
        {
            Transform hb = hitboxes[i];
            if (hb == null) continue;

            // World-space spawn so it stays put where the rope was; NOT parented to the hitbox.
            ParticleSystem fade = Instantiate(prefab, hb.position, hb.rotation);
            fade.gameObject.SetActive(true);
            fade.Play();

            // Auto-cleanup: prefer the system's own duration, capped by the safety lifetime.
            float life = Mathf.Min(fadeOutMaxLifetime, fade.main.duration + fade.main.startLifetime.constantMax);
            Destroy(fade.gameObject, life);
        }
    }

    private ParticleSystem PrefabForElement(AnchorElement? element)
    {
        return element switch
        {
            AnchorElement.Fire => fireVFX,
            AnchorElement.Ice => iceVFX,
            AnchorElement.Wind => windVFX,
            _ => null
        };
    }

    // Enables the effect matching 'element' on every hitbox and disables the other two.
    private void ApplyElement(AnchorElement? element)
    {
        currentElement = element;   // cache so a later detach knows what to fade out

        if (hitboxEffects == null) return;

        for (int i = 0; i < hitboxEffects.Length; i++)
        {
            ToggleEffect(hitboxEffects[i].fire, element == AnchorElement.Fire);
            ToggleEffect(hitboxEffects[i].ice, element == AnchorElement.Ice);
            ToggleEffect(hitboxEffects[i].wind, element == AnchorElement.Wind);
        }
    }

    private void ToggleEffect(ParticleSystem fx, bool on)
    {
        if (fx == null) return;
        if (fx.gameObject.activeSelf != on)
            fx.gameObject.SetActive(on);
    }

    private void SetHitboxesActive(bool value)
    {
        active = value;
        if (hitboxes == null) return;
        foreach (Transform t in hitboxes)
            if (t != null) t.gameObject.SetActive(value);
    }

    private void FixedUpdate()
    {
        if (!active || bodies == null) return;
        for (int i = 0; i < hitboxCount; i++)
        {
            float t0 = i / (float)hitboxCount;
            float t1 = (i + 1) / (float)hitboxCount;
            Vector3 a = tether.GetPointAt(t0);
            Vector3 b = tether.GetPointAt(t1);
            Vector3 dir = b - a;
            // MovePosition/MoveRotation on kinematic bodies gives proper swept movement so fast rope swings still register trigger contacts.
            bodies[i].MovePosition((a + b) * 0.5f);
            if (dir.sqrMagnitude > 1e-8f)
                bodies[i].MoveRotation(Quaternion.LookRotation(dir));
        }
    }
}