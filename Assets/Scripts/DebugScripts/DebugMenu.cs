using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    [Header("References")]
    public CombatStatistics statistics;
    public PlayerTakeDamage playerTakeDamage;
    public WaveRoundSystem waveRoundSystem;
    public UpgradeManager upgradeManager;
    public InventoryManager inventoryManager;
    public Transform[] teleportSpots;
    public GameObject player;

    [Header("Appearance")]
    public GUISkin skin;
    public Vector2 menuPosition = new Vector2(10, 10);
    public Vector2 menuSize = new Vector2(420, 160);

    [Header("Categories")]
    [Tooltip("Categories to display in the debug menu.")]
    public List<DebugCategory> debugCategories = new List<DebugCategory>();

    bool isOpen = false;
    int selectedCategory = 0;
    Vector2 scroll;

    void Awake()
    {
        if (debugCategories == null || debugCategories.Count == 0)
        {
            Debug.LogError("No Debug Categories assigned to the Debug Menu. Please add at least one category.");

        }
        AutoResolveTeleportSpots();
    }

    void AutoResolveTeleportSpots() //If TeleportSpots aren't assigned, this tries to find them. 
    {
        if (teleportSpots != null && teleportSpots.Length > 0)
            return;

        var tagged = GameObject.FindGameObjectsWithTag("TeleportSpot");
        if (tagged != null && tagged.Length > 0)
        {
            teleportSpots = tagged.Select(g => g.transform).ToArray();
            return;
        }

        var allTransforms = Object.FindObjectsOfType<Transform>();
        var matches = allTransforms.Where(t => t.name.StartsWith("TeleportSpot")).ToArray();
        if (matches != null && matches.Length > 0)
        {
            teleportSpots = matches;
            return;
        }
    }

    public void TeleportToZone(int index) //Teleports the player around to specified locations
    {
        if (teleportSpots == null || index < 0 || index >= teleportSpots.Length)
            return;

        Transform spot = teleportSpots[index];
        if (spot == null)
            return;

        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("TeleportToZone: player GameObject not assigned and no GameObject with tag 'Player' found.");
            return;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        MonoBehaviour playerMovement = player.GetComponent("PlayerMovement") as MonoBehaviour;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (rb != null)
        {
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            player.transform.position = spot.position;
            player.transform.rotation = spot.rotation;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = wasKinematic;
        }
        else
        {
            player.transform.position = spot.position;
            player.transform.rotation = spot.rotation;
        }

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
            isOpen = !isOpen;
        
    }

    void OnGUI() //Debug Menu's window. 
    {
        if (!isOpen) return;
        
        if (skin != null) GUI.skin = skin;

        Rect areaRect = new Rect(menuPosition.x, menuPosition.y, menuSize.x, menuSize.y);
        GUILayout.BeginArea(areaRect, GUI.skin.box);

        string[] titles = new string[debugCategories.Count];
        for (int i = 0; i < debugCategories.Count; i++)
            titles[i] = debugCategories[i] != null ? debugCategories[i].Title : $"Category {i}";

        if (titles.Length > 0)
            selectedCategory = GUILayout.Toolbar(selectedCategory, titles, GUILayout.Height(30));
        else
            GUILayout.Label("No categories assigned. Please add categories to the Debug Menu.");

        GUILayout.Space(6);
        GUILayout.Label($"Selected Category: {(debugCategories.Count > 0 ? titles[selectedCategory] : "None")}", GUI.skin.label);
        GUILayout.Space(8);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(menuSize.y - 90));

        if (debugCategories.Count > 0 && debugCategories[selectedCategory] != null)
            debugCategories[selectedCategory].Draw();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

}
