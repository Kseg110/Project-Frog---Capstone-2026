using UnityEngine;

[CreateAssetMenu(fileName = "DebugCategory", menuName = "Scriptable Objects/DebugCategory")]
public class DebugCategory : ScriptableObject
{
    public string Title = "New Category";

    public virtual void Draw()
    {
        GUILayout.Label($"No content defined for {Title}.");
    }
}
