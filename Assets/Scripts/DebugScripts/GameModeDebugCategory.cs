using System.Linq;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GameModeDebugCategory", menuName = "Scriptable Objects/GameModeDebugCategory")]
public class GameModeDebugCategory : DebugCategory
{
    [Header("References")]
    [Tooltip("Automatically resolved to the WaveRoundSystem in the active scene if left empty.")]
    public WaveRoundSystem waveRoundSystem;

    [Header("Wave Skip")]
    [SerializeField] private int skipToWaveNumber = 1;

    [Header("Upgrade Card Debug")]
    [SerializeField] private int selectedCardIndex = 0;
    private bool upgradeCardmenuOpen = false;

    private void OnEnable()
    {
        // Try to resolve automatically when the ScriptableObject is enabled
        if (waveRoundSystem == null)
            AutoFindWaveRoundSystem();
    }

    public override void Draw()
    {
        GUILayout.Label("Game Mode Debug", GUI.skin.box);

        // Ensure we have a reference before drawing the action button
        if (waveRoundSystem == null)
        {
            // Try again in case the scene changed or object became available
            AutoFindWaveRoundSystem();
        }

        if (waveRoundSystem == null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("WaveRoundSystem", GUILayout.Width(140));
            if (GUILayout.Button("Find In Scene", GUILayout.Width(140)))
            {
                AutoFindWaveRoundSystem();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("No WaveRoundSystem assigned.", GUI.skin.label);
            return;
        }

        // Show the assigned reference and action button
        GUILayout.BeginHorizontal();
        GUILayout.Label("Wave Controls", GUILayout.Width(140));
        if (GUILayout.Button("Kill All Enemies In Wave", GUILayout.Width(220)))
        {
            if (waveRoundSystem != null)
            {
                waveRoundSystem.KillAllEnemiesInWaveDebug();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        //Skip wave
        GUILayout.BeginHorizontal();
        GUILayout.Label("Skip To Wave", GUILayout.Width(140));
        string waveText = GUILayout.TextField(skipToWaveNumber.ToString(), GUILayout.Width(80));
        if (int.TryParse(waveText, out int parsedWave))
        {
            skipToWaveNumber = Mathf.Max(1, parsedWave);
        }

        if (GUILayout.Button("Skip", GUILayout.Width(100)))
        {
            if (waveRoundSystem != null)
            {
                waveRoundSystem.SkipToWave(skipToWaveNumber);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Upgrade Card Debug", GUI.skin.box);

        UpgradeManager upgradeManager = UpgradeManager.Instance;

        if (upgradeManager == null)
        {
            GUILayout.Label("UpgradeManager not found.");
        }
        else
        {
            List<UpgradeDataSO> cards = upgradeManager.GetAllCards();

            if (cards == null || cards.Count == 0)
            {
                GUILayout.Label("No upgrade cards found");
            } else
            {
                //Keep index valid if the card list changes
                selectedCardIndex = Mathf.Clamp(selectedCardIndex, 0, cards.Count - 1);

                UpgradeDataSO selectedCard = cards[selectedCardIndex];

                GUILayout.BeginHorizontal();

                GUILayout.Label("Card", GUILayout.Width(140));

#if UNITY_EDITOR
                if (GUILayout.Button(selectedCard.CardName, GUILayout.Width(220)))
                {
                    ShowCardDropdown(cards);
                }
#else
                GUILayout.Label(selectedCard.CardName, GUILayout.Width(220));
#endif

                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();

                GUILayout.Label("Current Level", GUILayout.Width(140));
                GUILayout.Label($"{upgradeManager.GetLevel(selectedCard)} / {selectedCard.MaxLevel}", GUILayout.Width(220));

                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(140));

                if (GUILayout.Button("Add Card", GUILayout.Width(105)))
                {
                    upgradeManager.DebugAddCard(selectedCard);
                }

                if (GUILayout.Button("Remove Card", GUILayout.Width(105)))
                {
                    upgradeManager.DebugremoveCard(selectedCard);
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    bool AutoFindWaveRoundSystem()
    {
        // Fast path: find active scene instances
        var found = Object.FindAnyObjectByType<WaveRoundSystem>();
        if (found != null)
        {
            waveRoundSystem = found;
            return true;
        }

        // Fallback: include inactive scene objects and assets
        var all = Resources.FindObjectsOfTypeAll<WaveRoundSystem>();
        if (all != null && all.Length > 0)
        {
            // Prefer scene instances (scene.isLoaded) over assets/prefabs
            var sceneInstance = all.FirstOrDefault(x =>
            {
                // Some returned objects may be assets; ensure we have a GameObject and a loaded scene
                return x != null && x.gameObject != null && x.gameObject.scene.isLoaded;
            });

            if (sceneInstance != null)
            {
                waveRoundSystem = sceneInstance;
                return true;
            }

            // If no scene instance, pick the first available (useful if there's only one)
            waveRoundSystem = all[0];
            return true;
        }

        // Nothing found
        waveRoundSystem = null;
        return false;
    }

#if UNITY_EDITOR
    private void ShowCardDropdown(List<UpgradeDataSO> cards)
    {
        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeDataSO card = cards[i];

            if (card == null)
                continue;

            int index = i;

            menu.AddItem(
                new GUIContent(card.CardName),
                index == selectedCardIndex,
                () =>
                {
                    selectedCardIndex = index;
                }
            );
        }

        menu.ShowAsContext();
    }
#endif
}

