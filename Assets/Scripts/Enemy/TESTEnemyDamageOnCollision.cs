using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TESTEnemyDamageOnCollision : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float knockbackDistance = 5f;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Terrain")) 
        { 
            Destroy(gameObject);
            return;
        }
        if (other.tag == "Player")
        {
            TryDamage(other);
        }
        if(other.tag != "projectile")
        {
           Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag != "Player") { return; }
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!CompareTag("Enemy")) return;

        PlayerTakeDamage playerTakeDamage = other.GetComponentInParent<PlayerTakeDamage>();

        if (playerTakeDamage == null) return;

        Vector3 knockDirection = other.transform.position - transform.position;
        knockDirection.y = 0f;
        knockDirection = knockDirection.normalized;

        playerTakeDamage.TryApplyDamageAndKnockback(damageAmount, knockDirection, knockbackDistance);
    }
}