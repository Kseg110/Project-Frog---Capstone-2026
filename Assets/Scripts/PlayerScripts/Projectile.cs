using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float baseSpeed = 10f;
    public float baseDamage = 10f;
    public float maxScale = 2f;

    public float speed;
    public float damage;

    public void Initialize(float chargePercent)
    {
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent);
        transform.localScale = Vector3.one * scale;
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}

