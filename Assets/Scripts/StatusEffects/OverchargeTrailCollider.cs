using UnityEngine;
using System;
using System.Collections.Generic;

public class OverchargeTrailCollider : MonoBehaviour
{
    public event Action<GameObject> OnEnemyHit;

    [Header("Collision Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float damageInterval = 1.0f; // for testing and prevents damage spamming
    [SerializeField] private GameObject colliderPrefab;
    [SerializeField] private float colliderRadius = 4f;
    [SerializeField] private int maxColliders = 12;
    [SerializeField] private float colliderSpacing = 0.3f;

    [Header("Element VFX")]
    [SerializeField] private ParticleSystem fireVFX;
    [SerializeField] private ParticleSystem iceVFX;
    [SerializeField] private ParticleSystem windVFX;

    private struct TrailEffects
    {
        public ParticleSystem fire;
        public ParticleSystem ice;
        public ParticleSystem wind;
    }
    private TrailEffects[] trailEffects;

    private TrailRenderer trailRenderer;
    private List<GameObject> activeColliders = new List<GameObject>();
    private Dictionary<GameObject, float> enemyDamageTimes = new Dictionary<GameObject, float>();
    private bool isEnabled = false;
    // The anchor type that determines which VFX to spawn on each collider
    private PlayerOvercharge.AnchorType currentAnchorType = PlayerOvercharge.AnchorType.None;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if(trailRenderer == null)
        {
            Debug.Log("[OverchargeTrailCollider] No TrailRenderer found on GameObject");
        }
    }

    private void Update()
    {
        if (isEnabled && trailRenderer != null)
        {
            UpdateTrailCollider();
        }
    }

    public void EnableCollider()
    {
        isEnabled = true;
    }

    public void DisableCollider()
    {
        isEnabled = false;
        ClearAllColliders();
        enemyDamageTimes.Clear();
    }

    private void UpdateTrailCollider()
    {
        // Get trail positions
        int positionCount = trailRenderer.positionCount;
        if (positionCount < 2) return;

        // Calculate how many colliders needed
        int neededColliders = Mathf.Min(positionCount, maxColliders); 

        // Create or reuse colliders
        while (activeColliders.Count < neededColliders)
        {
            GameObject newCollider = CreateColliderInstance();
            activeColliders.Add(newCollider);
        }

        // update collider postitions on trail
        for (int i = 0; i < neededColliders; i++)
        {
            int trailIndex = Mathf.FloorToInt((float)i / neededColliders * positionCount);
            trailIndex = Mathf.Min(trailIndex, positionCount - 1);

            Vector3 position = trailRenderer.GetPosition(trailIndex);
            activeColliders[i].transform.position = position;
            activeColliders[i].SetActive(true);
        }

        // Disable extra colliders
        for (int i = neededColliders; i < activeColliders.Count; i++)
        {
            activeColliders[i].SetActive(false);
        }
    }

    private GameObject CreateColliderInstance()
    {
        GameObject colliderObj = new GameObject("TrailCollider");
        colliderObj.transform.SetParent(transform);
        colliderObj.layer = gameObject.layer;

        SphereCollider sphere = colliderObj.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = colliderRadius;

        TrailColliderTrigger trigger = colliderObj.AddComponent<TrailColliderTrigger>();
        trigger.enemyTag = enemyTag;
        trigger.OnEnemyEnter += HandleEnemyTrigger;

        // Instantiate the matching VFX prefab as a child of the collider so it follows
        ParticleSystem vfxInstance = null;
        switch (currentAnchorType)
        {
            case PlayerOvercharge.AnchorType.Fire:
                if (fireVFX != null)
                    vfxInstance = Instantiate(fireVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.Ice:
                if (iceVFX != null)
                    vfxInstance = Instantiate(iceVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.Wind:
                if (windVFX != null)
                    vfxInstance = Instantiate(windVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.None:
            default:
                break;
        }

        if (vfxInstance != null)
        {
            // Reset local transform so VFX aligns with collider
            vfxInstance.transform.localPosition = Vector3.zero;
            vfxInstance.transform.localRotation = Quaternion.identity;
        }

        return colliderObj;
    }

    // Called by PlayerOvercharge to set which anchor type is currently overcharged
    public void SetAnchorType(PlayerOvercharge.AnchorType anchorType)
    {
        currentAnchorType = anchorType;
    }

    private void HandleEnemyTrigger(GameObject enemy)
    {
        // Damage interval check per enemy
        if (!enemyDamageTimes.ContainsKey(enemy))
        {
            enemyDamageTimes[enemy] = 0f;
        }

        if (Time.time - enemyDamageTimes[enemy] >= damageInterval)
        {
            enemyDamageTimes[enemy] = Time.time;
            OnEnemyHit?.Invoke(enemy);
        }
    }

    private void ClearAllColliders()
    {
        foreach(GameObject collider in activeColliders)
        {
            if (collider != null)
            {
                Destroy(collider);
            }
        }
        activeColliders.Clear();
    }

    private void OnDestroy()
    {
        ClearAllColliders();
    }
}

// Helper component for individual trail colliders
public class TrailColliderTrigger : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public event Action<GameObject> OnEnemyEnter;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(enemyTag))
        {
            OnEnemyEnter?.Invoke(other.gameObject);
        }
    }
}
