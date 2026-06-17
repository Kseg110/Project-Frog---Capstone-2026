using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private GameObject flyInventoryUIPrefab;
    [SerializeField] private PlayerTongueHealing playerTongueHealing;
    [SerializeField] private InputActionReference flyConsumeActionRef;
    [SerializeField] private int maximumInventorySize = 3;
    [SerializeField] private Vector2 flyIconSize = new Vector2(50f, 50f);

    public List<GameObject> flyInventoryUIPrefabList;

    public void GainFlyInInventory(int numberOfFlies)
    {
        while (numberOfFlies > 0)
        {
            // Add an instance of flyInventoryUIPrefab to Inventory as a child
            if (flyInventoryUIPrefabList.Count < maximumInventorySize)
            {
                GameObject newFly = Instantiate(flyInventoryUIPrefab, transform);
                
                RectTransform rectTransform = newFly.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = flyIconSize;
                }
                
                flyInventoryUIPrefabList.Add(newFly);
            }
            numberOfFlies--;
        }
    }


    private void OnEnable()
    {
        flyConsumeActionRef.action.Enable();
    }

    private void OnDisable()
    {
        flyConsumeActionRef.action.Disable();
    }

    private void Update()
    {
        // Check if the interaction button was pressed during this frame
        if (flyConsumeActionRef.action.WasPressedThisFrame())
        {
            ConsumeFly();
        }
    }

    private void ConsumeFly()
    {
        if (flyInventoryUIPrefabList == null || flyInventoryUIPrefabList.Count == 0)
        {
            return;
        }

        Destroy(flyInventoryUIPrefabList[^1]);
        flyInventoryUIPrefabList.RemoveAt(flyInventoryUIPrefabList.Count - 1);
        playerTongueHealing.HealPlayer(1);
    }
}