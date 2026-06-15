using System.Collections.Generic;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    [Header("References")]
    public CombatStatistics statistics;
    public PlayerTakeDamage playerTakeDamage;
    public WaveRoundSystem waveRoundSystem;

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
            isOpen = !isOpen;
        
    }

    void OnGUI()
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
