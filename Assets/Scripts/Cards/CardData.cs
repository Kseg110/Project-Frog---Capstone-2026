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
    [SerializeField] private string cardName;
    [SerializeField,TextArea] private string descriptionTemplate;

    [Header("Classification")]
    [SerializeField] private AnchorElement element;
    [SerializeField] private CardRarity rarity;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;

    [Header("Leveling")]
    // Example: [1,2,3] = 3 levels
    [SerializeField] private float[] levelValues;

    // Runtime tracking (not saved in asset)
    [HideInInspector][SerializeField] private int currentLevel = 0;

    public string CardName => cardName;
    public AnchorElement Element => element;
    public CardRarity Rarity => rarity;
    public Sprite Icon => icon;

    public int MaxLevel => levelValues != null ? levelValues.Length : 1;

    // Maxed when currentLevel == MaxLevel
    public bool IsMaxed => currentLevel >= MaxLevel;

    public int CurrentLevel
    {
        get => currentLevel;
        set => currentLevel = Mathf.Clamp(value, 0, MaxLevel);
    }

    public float CurrentValue
    {
        get
        {
            int index = Mathf.Clamp(currentLevel - 1, 0, MaxLevel - 1);
            return levelValues != null ? levelValues[index] : 0f;
        }
    }

    public float GetTotalValueUpToLevel(int level)
    {
        float total = 0f;

        level = Mathf.Clamp(level, 0, MaxLevel - 1);

        for (int i = 0; i <= level; i++)
            total += levelValues[i];

        return total;
    }

    public string GetDescriptionForLevel(int level)
    {
        float total = GetTotalValueUpToLevel(level);
        return descriptionTemplate.Replace("{value}", total.ToString());
    }
}