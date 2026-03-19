using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to a GameObject that acts as the visual indicator above an enemy's head.
/// The indicator fades in, transitions from Yellow → Orange → Red over a configurable
/// wind-up duration, then fires a callback so the enemy can release the attack.
///
/// Setup:
///   1. Create a child GameObject on your Enemy (e.g. a quad, sprite, or UI canvas in world space).
///   2. Attach this script to that child GameObject.
///   3. Assign a SpriteRenderer or MeshRenderer — the script auto-detects either.
///   4. Call indicator.TriggerWindup(duration, onComplete) from your EnemyAttackController.
/// </summary>
public class AttackIndicator : MonoBehaviour
{
    [Header("Colours")]
    [Tooltip("Starting colour at the beginning of the wind-up.")]
    [SerializeField] private Color warningColor  = new Color(1f, 0.92f, 0.016f, 1f); // Yellow
    [Tooltip("Colour at the midpoint of the wind-up.")]
    [SerializeField] private Color midColor      = new Color(1f, 0.5f,  0f,     1f); // Orange
    [Tooltip("Final colour when the attack fires.")]
    [SerializeField] private Color dangerColor   = new Color(1f, 0.1f,  0.1f,   1f); // Red

    [Header("Animation")]
    [Tooltip("How much the indicator pulses in scale at the end of wind-up (0 = no pulse).")]
    [SerializeField] private float endPulseScale   = 1.35f;
    [Tooltip("Duration of the end-pulse scale animation in seconds.")]
    [SerializeField] private float endPulseDuration = 0.12f;
    [Tooltip("Should the indicator bob up and down while active?")]
    [SerializeField] private bool  enableBob        = true;
    [SerializeField] private float bobAmplitude     = 0.04f;
    [SerializeField] private float bobFrequency     = 6f;

    [Header("Offset")]
    [Tooltip("Position offset relative to the enemy transform (set Y to clear the head).")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    // Private 
    private SpriteRenderer _spriteRenderer;
    private Renderer        _meshRenderer;
    private bool            _hasSpriteRenderer;

    private Vector3   _baseLocalPosition;
    private Vector3   _baseLocalScale;
    private Coroutine _activeRoutine;

    private void Awake()
    {
        _spriteRenderer    = GetComponent<SpriteRenderer>();
        _hasSpriteRenderer = _spriteRenderer != null;

        if (!_hasSpriteRenderer)
            _meshRenderer = GetComponent<Renderer>();

        _baseLocalPosition = transform.localPosition;
        _baseLocalScale    = transform.localScale;

        // Start hidden
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Keep indicator at the correct world offset from its parent enemy
        if (transform.parent != null)
            transform.position = transform.parent.position + worldOffset;
    }

    // Public API

    /// <summary>
    /// Begin the wind-up sequence.
    /// </summary>
    /// <param name="duration">Total wind-up time in seconds before <paramref name="onComplete"/> fires.</param>
    /// <param name="onComplete">Callback invoked when the wind-up finishes (trigger the actual attack here).</param>
    public void TriggerWindup(float duration, System.Action onComplete = null)
    {
        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        gameObject.SetActive(true);
        transform.localScale = _baseLocalScale;
        _activeRoutine = StartCoroutine(WindupRoutine(duration, onComplete));
    }

    /// <summary>
    /// Immediately hide the indicator (call after the attack resolves).
    /// </summary>
    public void Hide()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
        gameObject.SetActive(false);
        transform.localScale = _baseLocalScale;
        SetAlpha(0f);
    }

    // Coroutines 

    private IEnumerator WindupRoutine(float duration, System.Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Colour: Yellow → Orange → Red using two-segment lerp
            Color current;
            if (t < 0.5f)
                current = Color.Lerp(warningColor, midColor, t * 2f);
            else
                current = Color.Lerp(midColor, dangerColor, (t - 0.5f) * 2f);

            // Fade in quickly over the first 15% of the wind-up
            current.a = Mathf.Clamp01(t / 0.15f);
            SetColor(current);

            // Bob
            if (enableBob)
            {
                float bobOffset = Mathf.Sin(elapsed * bobFrequency) * bobAmplitude;
                transform.localPosition = _baseLocalPosition + new Vector3(0f, bobOffset, 0f);
            }

            yield return null;
        }

        // Snap to full danger colour
        SetColor(dangerColor);

        // End pulse
        yield return StartCoroutine(PulseRoutine());

        // Fire the attack callback
        onComplete?.Invoke();

        // Brief linger so the player can register the hit, then hide
        yield return new WaitForSeconds(0.08f);
        Hide();
    }

    private IEnumerator PulseRoutine()
    {
        float half    = endPulseDuration * 0.5f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.Lerp(_baseLocalScale, _baseLocalScale * endPulseScale, t);
            yield return null;
        }

        elapsed = 0f;

        // Scale back down
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.Lerp(_baseLocalScale * endPulseScale, _baseLocalScale, t);
            yield return null;
        }

        transform.localScale = _baseLocalScale;
    }

    // Helpers 

    private void SetColor(Color color)
    {
        if (_hasSpriteRenderer)
            _spriteRenderer.color = color;
        else if (_meshRenderer != null)
            _meshRenderer.material.color = color;
    }

    private void SetAlpha(float alpha)
    {
        if (_hasSpriteRenderer)
        {
            Color c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }
        else if (_meshRenderer != null)
        {
            Color c = _meshRenderer.material.color;
            c.a = alpha;
            _meshRenderer.material.color = c;
        }
    }
}
