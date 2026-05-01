using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float baseSpeed = 10f; //Adjust as nessisary 
    [SerializeField] protected float baseDamage = 10f; //Adjust as nessisary 
    [SerializeField] protected float maxScale = 2f; //Adjust as nessisary 

    public float speed;
    public float damage;

    public string effectType;
    public float effectDuration;
    public float effectValue;

    public virtual void Initialize(float chargePercent)
    {
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent); //Only exists to help visualize the charge's effect on the projectile in the absence of damage
        transform.localScale = Vector3.one * scale;

        Destroy(gameObject, 3f);
    }

    protected virtual void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Does Collided object implement IDamageable
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (!string.IsNullOrEmpty(effectType))
            {
                damageable.TakeDmg(damage, effectType, effectDuration, effectValue);
            }
            else
            {
                damageable.TakeDmg(damage);
            }                
            //Destroy(gameObject); // Destroy projectile commented out for now
        }
    }
}

