using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    [Header("Melee ring")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private int maxEnemiesPerSlot = 1;
    [Header("Ranged ring")]
    [SerializeField] private Transform[] rangedSlots;
    [SerializeField] private int maxEnemiesPerSlotRanged = 3;

    [SerializeField] private Transform player;

    [Header("Movement settings")]
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Rotation settings")]
    [SerializeField] private float rotationAmount = 5f;
    [SerializeField] private float rotationSpeed = 1.5f;

    private Quaternion startingRotation;
    private Vector3 velocity;

    private int[] enemies;
    private int[] rangedEnemies;
    private Dictionary<MovementComponent, SlotInfo> enemySlots = new();


    private class SlotInfo
    {
        public int index;
        public bool isRanged;
        public SlotInfo(int index, bool isRanged)
        {
            this.index = index;
            this.isRanged = isRanged;
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        startingRotation = transform.localRotation;

        enemies = new int[slots.Length];
        rangedEnemies = new int[rangedSlots.Length];

        player = GameObject.Find("Player").transform;
    }

    private void FindPlayer()
    {
        player = GameObject.Find("Player").transform;
    }

    private void Update()
    {
        if(player == null)
        {
            FindPlayer();
            return;
        }
        //move to player
        transform.position = Vector3.SmoothDamp(transform.position, player.position, ref velocity, smoothTime);

        //rotate parent slot to rotate all slots
        float rotation = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;

        transform.localRotation = startingRotation * Quaternion.Euler(0f, rotation, 0f);

    }

    public Transform RequestSlot(MovementComponent enemy)
    {
        int previousSlot = -1;
        bool previousWasRanged = false;

        if (enemySlots.TryGetValue(enemy, out SlotInfo currentSlotInfo))
        {
            previousSlot = currentSlotInfo.index;
            previousWasRanged = currentSlotInfo.isRanged;

            ReleaseSlot(enemy);
        }
        //Try to get in the melee ring first
        List<int> availableSlots = new List<int>();

        // Find a slot(cannot be previous slot)
        for (int i = 0; i < slots.Length; i++)
        {
            if (!previousWasRanged && i == previousSlot)
            {
                continue;
            }

            if (enemies[i] < maxEnemiesPerSlot)
            {
                availableSlots.Add(i);
            }
        }

        if (availableSlots.Count > 0)
        {
            int chosenSlot =
                availableSlots[Random.Range(0, availableSlots.Count)];

            enemies[chosenSlot]++;
            enemySlots.Add(enemy, new SlotInfo(chosenSlot, false));

            return slots[chosenSlot];
        }

        //Once all melee slots are used, assign ranged slots
        List<int> availableOuterSlots = new List<int>();

        for (int i = 0; i < rangedSlots.Length; i++)
        {
            if (previousWasRanged && i == previousSlot)
            {
                continue;
            }

            if (rangedEnemies[i] < maxEnemiesPerSlot)
            {
                availableOuterSlots.Add(i);
            }
        }

        if (availableOuterSlots.Count > 0)
        {
            int chosenSlot = availableOuterSlots[Random.Range(0, availableOuterSlots.Count)];

            rangedEnemies[chosenSlot]++;
            enemySlots.Add(enemy, new SlotInfo(chosenSlot, true));

            return rangedSlots[chosenSlot];
        }
        //if both rings are full return null
        Debug.LogWarning("TARGET MANAGER HAS RUN OUT OF AVAILABLE SLOTS//");    
        return null;
    }
    public void ReleaseSlot(MovementComponent enemy)
    {
        if (!enemySlots.TryGetValue(enemy, out SlotInfo slot))
            return;

        if (slot.isRanged)
        {
            rangedEnemies[slot.index]--;
        }
        else
        {
            enemies[slot.index]--;
        }

        enemySlots.Remove(enemy);

        if(!slot.isRanged)
        {
            PromoteOuterEnemy();
        }
    }

    //call this when a slot is freed to ensure the melee ring is always full (if there are enough enemies to fill it//
    private void PromoteOuterEnemy()
    {
        List<int> availableInnerSlots = new List<int>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (enemies[i] < maxEnemiesPerSlot)
            {
                availableInnerSlots.Add(i);
            }
        }
        //if no slots available, return
        if (availableInnerSlots.Count == 0)
            return;

        MovementComponent enemyToPromote = null;
        SlotInfo outerSlot = null;

        foreach (var pair in enemySlots)
        {
            if (pair.Value.isRanged)
            {
                enemyToPromote = pair.Key;
                outerSlot = pair.Value;
                break;
            }
        }
        //if no enemy to send into melee, return
        if (enemyToPromote == null)
            return;

        // Pick an available inner slot
        int newSlot = availableInnerSlots[Random.Range(0, availableInnerSlots.Count)];

        // Free the enemy's old outer slot
        rangedEnemies[outerSlot.index]--;

        // Occupy the new inner slot
        enemies[newSlot]++;

        // Update the enemy's slot information
        enemySlots[enemyToPromote] = new SlotInfo(newSlot, false);

        // Tell the enemy to move toward its new target
        enemyToPromote.SetTarget(slots[newSlot]);
    }
}
