using UnityEngine;

[RequireComponent(typeof(Collider))]
/// <summary>
/// Simple projectile that moves forward and can optionally apply a poison status effect on impact.
/// </summary>
public class DartProjectile : MonoBehaviour
{
    [SerializeField]
    private float speed = 10f;

    [SerializeField]
    private bool canApplyPoison;

    /// <summary>
    /// Gets or sets a value indicating whether this dart can apply poison on hit.
    /// </summary>
    public bool CanApplyPoison
    {
        get
        {
            return canApplyPoison;
        }
        set
        {
            canApplyPoison = value;
        }
    }

    private void Awake()
    {
        Collider projectileCollider = GetComponent<Collider>();

        if (projectileCollider == null)
        {
            Debug.LogError($"Class {nameof(DartProjectile)} requires a Collider component.");
        }
    }

    private void Update()
    {
        MoveForward();
    }

    /// <summary>
    /// Moves the projectile forward at a constant speed.
    /// </summary>
    private void MoveForward()
    {
        Vector3 movement = Vector3.forward * speed * Time.deltaTime;
        transform.Translate(movement);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    /// <summary>
    /// Handles collision logic, optionally applying poison and destroying the projectile.
    /// </summary>
    private void HandleCollision(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (canApplyPoison)
        {
            StatusEffectHandler statusEffectHandler = other.GetComponent<StatusEffectHandler>();

            if (statusEffectHandler != null)
            {
                statusEffectHandler.ApplyEffect(new PoisonEffect());
            }
        }

        Destroy(gameObject);
    }
}