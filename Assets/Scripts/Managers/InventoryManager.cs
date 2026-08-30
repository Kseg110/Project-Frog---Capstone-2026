using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Assets.Scripts.Player;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private GameObject flyInventoryUIPrefab;
    [SerializeField] private PlayerTongueHealing playerTongueHealing;
    [SerializeField] private InputActionReference flyConsumeActionRef;
    [SerializeField] private InputActionReference flyConsumeActionRefGP;
    [SerializeField] private int maximumInventorySize = 3;
    [SerializeField] private Vector2 flyIconSize = new Vector2(50f, 50f);
    public List<GameObject> flyInventoryUIPrefabList;

    private int currentFlyCount = 0;

    [SerializeField] private PlayerAnimation playerAnimation;


    public void GainFlyInInventory(int numberOfFlies)
    {
        if (flyInventoryUIPrefabList == null)
            flyInventoryUIPrefabList = new List<GameObject>();

        // Increase the held flies up to the maximum inventory size
        int spaceLeft = maximumInventorySize - currentFlyCount;
        int toAdd = Mathf.Clamp(numberOfFlies, 0, spaceLeft);
        currentFlyCount += toAdd;

        UpdateUI();
        Debug.Log($"After Pickup: {currentFlyCount}");
    }

    private void OnEnable()
    {
        flyConsumeActionRef.action.Enable();
        flyConsumeActionRefGP.action.Enable();
    }

    private void OnDisable()
    {
        flyConsumeActionRef.action.Disable();
        flyConsumeActionRefGP.action.Disable();
    }

    private void Update()
    {
        // Check if the interaction button was pressed during this frame
        if (flyConsumeActionRef.action.WasPressedThisFrame() || flyConsumeActionRefGP.action.WasPressedThisFrame())
        {
            Debug.Log("Consume button pressed");
            Debug.Log($"Pressed on {gameObject.name} ({GetInstanceID()})");
            Debug.Log($"[{GetInstanceID()}] Before: {currentFlyCount}");
            ConsumeFly();
        }
    }

    private void ConsumeFly()
    {
        Debug.Log($"Before Consume: {currentFlyCount}");
        if (currentFlyCount <= 0)
        {
            return;
        }

        currentFlyCount--;
        Debug.Log($"After Consume: {currentFlyCount}");

        UpdateUI();

        playerAnimation.PlayEatFly();
        playerTongueHealing.HealPlayer(1);
        playerTongueHealing.ApplyHealSlow(); // slow only fires on a successful consume
    }

    private void Awake()
    {
        // Ensure list is not null
        if (flyInventoryUIPrefabList == null)
            flyInventoryUIPrefabList = new List<GameObject>();

        // If the list wasn't populated in the inspector, try to gather children as UI slots
        if (flyInventoryUIPrefabList.Count == 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                flyInventoryUIPrefabList.Add(child);
            }
        }

        // Make sure UI reflects initial state (no flies held)
        currentFlyCount = Mathf.Clamp(currentFlyCount, 0, maximumInventorySize);
        UpdateUI();
    }

    // Enable/disable UI elements to match held flies
    private void UpdateUI()
    {
        if (flyInventoryUIPrefabList == null)
            return;

        for (int i = 0; i < flyInventoryUIPrefabList.Count; i++)
        {
            var go = flyInventoryUIPrefabList[i];
            if (go == null) continue;

            // Enable slots whose index is less than currentFlyCount
            go.SetActive(i < currentFlyCount);
        }
    }
}