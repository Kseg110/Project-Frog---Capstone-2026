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
            Debug.Log("CrocProjectile hit terrain and was destroyed.");
            Destroy(gameObject);
            return;
        }
        if (other.tag == "Player")
        {
            TryDamage(other);
            Debug.Log("CrocProjectile hit player.");
        }
        //if(other.tag != "projectile")
        //{
        //    Debug.Log($"CrocProjectile hit: {other.gameObject.name} | Tag: {other.tag} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        //    Destroy(gameObject);
        //}
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag != "Player") { return; }
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerTakeDamage playerTakeDamage = other.GetComponentInParent<PlayerTakeDamage>();

        if (playerTakeDamage == null) return;

        Vector3 knockDirection = other.transform.position - transform.position;
        knockDirection.y = 0f;
        knockDirection = knockDirection.normalized;

        playerTakeDamage.TryApplyDamageAndKnockback(damageAmount, knockDirection, knockbackDistance);
    }
}