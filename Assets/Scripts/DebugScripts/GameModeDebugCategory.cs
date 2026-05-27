using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameModeDebugCategory", menuName = "Scriptable Objects/GameModeDebugCategory")]
public class GameModeDebugCategory : DebugCategory
{
    [Header("References")]
    [Tooltip("Automatically resolved to the WaveRoundSystem in the active scene if left empty.")]
    public WaveRoundSystem waveRoundSystem;

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
        GUILayout.Label($"WaveRoundSystem: {waveRoundSystem.name}", GUI.skin.label);
    }

    bool AutoFindWaveRoundSystem()
    {
        // Fast path: find active scene instances
        var found = Object.FindObjectOfType<WaveRoundSystem>();
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
}

