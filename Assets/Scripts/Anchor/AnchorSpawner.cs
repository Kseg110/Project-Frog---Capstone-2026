using System.Collections.Generic;
using UnityEngine;

// Places a random subset of anchor prefabs at a random subset of spawn points. Put this on a single parent object; parent the spawn-point empties under it. -E.M
public class AnchorSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("If true, uses this object's direct children as spawn points, ignoring the manual array below.")]
    [SerializeField] private bool useChildrenAsPoints = true;
    [Tooltip("Manual list of spawn points. Used only when 'useChildrenAsPoints' is off.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Anchors")]
    [Tooltip("The Anchor prefab GameObjects to place (e.g. FireAnchor, IceAnchor, WindAnchor prefabs).")]
    [SerializeField] private GameObject[] anchorPrefabs;
    [Tooltip("How many anchors to place. Capped by the number of points and prefabs available.")]
    [SerializeField] private int anchorsToPlace = 3;

    [Header("Testing")]
    [Tooltip("Spawn automatically on Start. Turn off once a real system (e.g. wave manager) drives spawning.")]
    [SerializeField] private bool spawnOnStart = true;

    // Reusable buffers so we don't allocate fresh lists every spawn.
    private readonly List<int> pointIndices = new List<int>();
    private readonly List<AnchorBase> spawned = new List<AnchorBase>();

    public IReadOnlyList<AnchorBase> Spawned => spawned;

    private void Awake()
    {
        if (useChildrenAsPoints)
        {
            int count = transform.childCount;
            spawnPoints = new Transform[count];
            for (int i = 0; i < count; i++)
                spawnPoints[i] = transform.GetChild(i);
        }
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnAnchors();
    }

    public void SpawnAnchors()
    {
        Debug.Log($"[AnchorSpawner] SpawnAnchors called. points={spawnPoints?.Length ?? 0}, prefabs={anchorPrefabs?.Length ?? 0}, toPlace={anchorsToPlace}");

        ClearSpawned();

        if (spawnPoints == null || anchorPrefabs == null)
        {
            Debug.LogWarning("[AnchorSpawner] spawnPoints or anchorPrefabs is null — nothing to spawn.", this);
            return;
        }

        int placeCount = Mathf.Min(anchorsToPlace, spawnPoints.Length, anchorPrefabs.Length);
        Debug.Log($"[AnchorSpawner] placeCount resolved to {placeCount}");

        if (placeCount <= 0)
        {
            Debug.LogWarning("[AnchorSpawner] placeCount is 0 — check that points and prefabs are populated.", this);
            return;
        }

        // Build [0,1,2,...], then partial Fisher-Yates to pull 'placeCount' DISTINCT points.
        pointIndices.Clear();
        for (int i = 0; i < spawnPoints.Length; i++) pointIndices.Add(i);

        for (int i = 0; i < placeCount; i++)
        {
            int swap = Random.Range(i, pointIndices.Count);
            (pointIndices[i], pointIndices[swap]) = (pointIndices[swap], pointIndices[i]);

            Transform point = spawnPoints[pointIndices[i]];
            GameObject prefab = anchorPrefabs[i];

            if (point == null)
            {
                Debug.LogWarning($"[AnchorSpawner] Spawn point at index {pointIndices[i]} is null — skipping.", this);
                continue;
            }
            if (prefab == null)
            {
                Debug.LogWarning($"[AnchorSpawner] Anchor prefab at index {i} is null — skipping.", this);
                continue;
            }

            GameObject instance = Instantiate(prefab, point.position, point.rotation);

            // Grab the AnchorBase off the spawned object for tracking / refresh.
            AnchorBase anchor = instance.GetComponent<AnchorBase>();
            if (anchor != null)
                spawned.Add(anchor);
            else
                Debug.LogWarning($"[AnchorSpawner] Spawned '{prefab.name}' has no AnchorBase component.", instance);
        }

        Debug.Log($"[AnchorSpawner] Spawned {spawned.Count} anchor(s).");

        // Spawned anchors didn't exist when PlayerAnchor cached its list in Awake — refresh it.
        var playerAnchor = FindAnyObjectByType<PlayerAnchor>();
        if (playerAnchor != null) playerAnchor.RefreshAnchors();

    }

    public void ClearSpawned()
    {
        foreach (var a in spawned)
            if (a != null) Destroy(a.gameObject);
        spawned.Clear();
    }
}