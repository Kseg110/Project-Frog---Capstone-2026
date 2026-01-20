using UnityEngine;
using UnityEngine.InputSystem;

public class AttackScript : MonoBehaviour
{
    [Header("Charge Settings")]
    public float maxChargeTime = 2f;
    public float minChargeTime = 0.1f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private PlayerInputActions input;
    private float chargeTimer;
    private bool isCharging;
    [SerializeField] private FrogTongue frogTongue;

    private void Awake()
    {
        input = new PlayerInputActions();

        input.Player.Shoot.started += ctx => StartCharging();
        input.Player.Shoot.canceled += ctx => ReleaseCharging();

        if (frogTongue == null)
        {
            frogTongue = GetComponentInChildren<FrogTongue>();
            if (frogTongue == null)
            {
                Debug.LogError("FrogTongue reference not found on this GameObject");
            }
        }
    }

    private void OnEnable()
    {
        if (frogTongue != null)
        {
            input.Player.Tongue.started += ctx => frogTongue.BeginTongue();
            input.Player.Tongue.canceled += ctx => frogTongue.EndTongue();
        }

        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    void Update()
    {
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;
    }

    private void ReleaseCharging()
    {
        if (!isCharging) return;

        isCharging = false;

        float chargePercent = Mathf.Clamp01(chargeTimer / maxChargeTime);

        FireProjectile(chargePercent);
    }

    private void FireProjectile(float chargePercent)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(chargePercent);
        }
    }
}
