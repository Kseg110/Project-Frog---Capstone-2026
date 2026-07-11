using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }
    [SerializeField] private Transform[] slots;
    [SerializeField] private int maxEnemiesPerSlot = 1;
    [SerializeField] private Transform player; //only use to move the target slots DO NOT DIRECTLY TARGET THE PLAYER

    [Header("Movement settings")]
    [SerializeField] private float smoothTime = 0.2f; //adjust to make this move faster towards the player the further they are (farther player = faster tracking, slows down as it nears the player)//

    [Header("Rotation settings")]
    [SerializeField] private float rotationAmount = 5f;
    [SerializeField] private float rotationSpeed = 1.5f;

    private Quaternion startingRotation;



    private Vector3 velocity;

    private int[] enemies;
    private Dictionary<MovementComponent, int> enemySlots = new();

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
    
        //if already in a slot, remove it and stop it from being selected again//
        if (enemySlots.TryGetValue(enemy, out int currentIndex))
        {
            previousSlot = currentIndex;
            enemies[currentIndex]--;
            enemySlots.Remove(enemy);
        }

        List<int> availableSlots = new List<int>();

        // Find a slot(cannot be previous slot)
        for (int i = 0; i < slots.Length; i++)
        {
            if (i != previousSlot && enemies[i] < maxEnemiesPerSlot)
            {
                availableSlots.Add(i);
            }
        }
        //(unless it's the only slot left)
        if (availableSlots.Count == 0 && previousSlot != -1 && enemies[previousSlot] < maxEnemiesPerSlot)
        {
            availableSlots.Add(previousSlot);
        }

        //(or no more slots available)
        if (availableSlots.Count == 0)
            return null;

        // Pick a random available slot
        int chosenSlot = availableSlots[Random.Range(0, availableSlots.Count)];

        enemies[chosenSlot]++;
        enemySlots.Add(enemy, chosenSlot);

        return slots[chosenSlot];
    }
    public void ReleaseSlot(MovementComponent enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int index))
        {
            enemies[index]--;
            enemySlots.Remove(enemy);
        }
    }
}
