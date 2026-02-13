using UnityEngine;

public enum AnchorElement
{
    Fire,
    Ice,
    Wind
}

public enum CardRarity
{
    Common,
    Rare
}

[CreateAssetMenu(menuName = "ProjectFrog/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    [TextArea] public string description;

    [Header("Classification")]
    public AnchorElement element;
    public CardRarity rarity;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Leveling")]
    // Example: [1,2,3] = 3 levels
    public float[] levelValues;

    // Runtime tracking (not saved in asset)
    [HideInInspector] public int currentLevel = 0;

    public int MaxLevel => levelValues != null ? levelValues.Length : 1;

    public bool IsMaxed => currentLevel >= MaxLevel;

    public float CurrentValue => levelValues != null && currentLevel < levelValues.Length
        ? levelValues[currentLevel]
        : 0f;
}
