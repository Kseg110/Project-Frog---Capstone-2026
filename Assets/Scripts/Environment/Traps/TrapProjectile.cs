using UnityEngine;

//attach to projectile prefab spawned by trap when charged, handles movement and damage on hit

[RequireComponent(typeof(Collider))]
public class TrapProjectile : Projectile
{

	public enum TargetMode
	{ Player, Enemy, Both }

	[Header("Collision")]
	[Tooltip("Choose whether this projectile damages Player, Enemy, or Both.")]
	[SerializeField] private TargetMode targetMode = TargetMode.Enemy;
	[SerializeField] private bool destroyOnHit = true;

	private void Awake()
	{
		var col = GetComponent<Collider>();
		if (col == null)
			col = gameObject.AddComponent<BoxCollider>();
		col.isTrigger = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		HandleHit(other);
	}

	private void OnCollisionEnter(Collision collision)
	{
		HandleHit(collision.collider);
	}

	void HandleHit(Collider other)
	{
		if (other == null) return;

		bool dealtDamage = false;

		// PLAYER: prefer component lookup so child hitboxes work even if tag isn't directly on the collider
		if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
		{
			var playerMovement = other.GetComponentInParent<PlayerMovement>();
			if (playerMovement != null || other.gameObject.CompareTag("Player"))
			{
				Health health = null;
				if (playerMovement != null)
					health = playerMovement.GetComponent<Health>() ?? playerMovement.GetComponentInChildren<Health>();
				if (health == null)
					health = other.GetComponentInParent<Health>();

				if (health != null)
				{
					health.TakeDmg(damage);
					dealtDamage = true;
				}
				else
				{
					Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Player but no Health component found on {other.name}.");
					dealtDamage = true; // consider it handled to avoid hitting enemy branch when same object is tagged differently
				}
			}
		}

		// ENEMY
		if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both) && !dealtDamage)
		{
			// Prefer EnemyBase, then IDamageable, EnemyHealth, then tag fallback
			if (other.TryGetComponent<EnemyBase>(out var enemyBase))
			{
				if (enemyBase is IDamageable dmgable)
				{
					dmgable.TakeDmg(damage);
				}
				else
				{
					var enemyHealth = enemyBase.GetComponent<EnemyHealth>();
					if (enemyHealth != null)
						enemyHealth.TakeDamage(damage);
					else
					{
						var fallback = enemyBase.GetComponentInParent<Health>();
						if (fallback != null)
							fallback.TakeDmg(damage);
						else
							Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Enemy but no damageable component found on {other.name}.");
					}
				}

				dealtDamage = true;
			}
			else
			{
				var parentEnemy = other.GetComponentInParent<EnemyBase>();
				if (parentEnemy != null)
				{
					if (parentEnemy is IDamageable pdmg)
						pdmg.TakeDmg(damage);
					else
					{
						var eh = parentEnemy.GetComponent<EnemyHealth>();
						if (eh != null) eh.TakeDamage(damage);
						else
						{
							var fallback = parentEnemy.GetComponentInParent<Health>();
							if (fallback != null) fallback.TakeDmg(damage);
							else Debug.LogWarning($"[{nameof(TrapProjectile)}] Hit Enemy but no damageable component found on {other.name}.");
						}
					}
					dealtDamage = true;
				}
				else
				{
					if (other.TryGetComponent<IDamageable>(out var anyDmg))
					{
						anyDmg.TakeDmg(damage);
						dealtDamage = true;
					}
					else if (other.gameObject.CompareTag("Enemy"))
					{
						var fallbackHealth = other.GetComponentInParent<Health>();
						if (fallbackHealth != null)
						{
							fallbackHealth.TakeDmg(damage);
							dealtDamage = true;
						}
					}
				}
			}
		}

		if (dealtDamage && destroyOnHit)
		{
			Destroy(gameObject);
		}
	}
}
