using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores runtime card levels and handles random draws.
/// Support upgrades per anchors, player stats, and other card types in the future.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    [Header("All Cards")]
    [SerializeField] private List<UpgradeDataSO> allCards;
    private List<UpgradeDataSO> deck; //runtime pool of cards that can be drawn from, starts as a copy of allCards and shrinks as cards reach max level

    [Header("Rarity Chances")]
    [SerializeField, Range(0, 100)] private int commonChance = 90;
    [SerializeField, Range(0, 100)] private int rareChance = 10;

    // Runtime state: UpgradeDataSO -> level
    private Dictionary<UpgradeDataSO, int> cardLevels = new Dictionary<UpgradeDataSO, int>();
    private Dictionary<AnchorElement, Dictionary<UpgradeDataSO, int>> elementLevels;

    public event System.Action OnUpgradesChanged;

    // Singleton
    public static UpgradeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeElementDictionaries();
        ResetAllCards();
    }

    private void InitializeElementDictionaries()
    {
        elementLevels = new Dictionary<AnchorElement, Dictionary<UpgradeDataSO, int>>
        {
            { AnchorElement.Fire, new Dictionary<UpgradeDataSO, int>() },
            { AnchorElement.Ice, new Dictionary<UpgradeDataSO, int>() },
            { AnchorElement.Wind, new Dictionary<UpgradeDataSO, int>() }
        };
    }

    public void ResetAllCards()
    {
        // Reset UI-levels
        cardLevels.Clear();

        // Reset element-levels
        foreach (var element in elementLevels.Keys)
            elementLevels[element].Clear();

        foreach (var card in allCards)
        {
            cardLevels[card] = 0;

            // Initialize per-element dictionary
            elementLevels[card.Element][card] = 0;
        }

        // Reset deck
        deck = new List<UpgradeDataSO>(allCards);
    }

    /// <summary>
    /// Get current runtime level of a card.
    /// </summary>
    public int GetLevel(UpgradeDataSO card)
    {
        if (card == null) return 0;
        cardLevels.TryGetValue(card, out int level);
        //Debug.Log($"[{GetInstanceID()}] Card {card.CardName} is level {level}");
        return level; // returns 0 if not found   
    }

    /// <summary>
    /// Draw a set of random cards for player to choose from.
    /// </summary>
    public List<UpgradeDataSO> GetRandomCards(int count)
    {
        List<UpgradeDataSO> result = new List<UpgradeDataSO>();

        // Temporary pool to avoid duplicates
        List<UpgradeDataSO> tempPool = new List<UpgradeDataSO>(deck);

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0)
                break;

            UpgradeDataSO card = DrawOneCard(tempPool);
            result.Add(card);
            tempPool.Remove(card);
        }

        return result;
    }

    /// <summary>
    /// Choose a card based on rarity chances
    /// </summary>
    private UpgradeDataSO DrawOneCard(List<UpgradeDataSO> pool)
    {
        int roll = Random.Range(0, 100);
        CardRarity targetRarity = (roll < rareChance) ? CardRarity.Rare : CardRarity.Common;

        // Try rarity pool
        List<UpgradeDataSO> rarityPool = pool.FindAll(c => c.Rarity == targetRarity);

        if (rarityPool.Count == 0)
            rarityPool = pool; // fallback

        return rarityPool[Random.Range(0, rarityPool.Count)];
    }

    /// <summary>
    /// Apply a chosen card: increment its runtime level
    /// </summary>
    public void OnCardChosen(UpgradeDataSO card)
    {
        if (card == null) return;

        // Ensure the card is tracked in the dictionary
        if (!cardLevels.ContainsKey(card)) cardLevels[card] = 0;

        // Increment the card's level
        cardLevels[card]++;

        // Element tracking
        var element = card.Element;
        elementLevels[element][card]++;

        // Remove the card from the pool if it reaches max level
        if (cardLevels[card] >= card.MaxLevel -1)
        {
            deck.Remove(card);
        }

        // Notify Anchors
        OnUpgradesChanged?.Invoke();

        FindFirstObjectByType<CardIconManager>().RefreshIcons();
    }

    /// <summary>
    /// Returns the total accumulated stat value for a given element.
    /// </summary>
    public float GetTotalStatForElement(AnchorElement element, UpgradeStat stat)
    {
        float total = 0f;

        foreach (var kvp in elementLevels[element])
        {
            if (kvp.Key.Stat == stat)
                total += kvp.Key.GetTotalValueUpToLevel(kvp.Value);
        }

        return total;
    }

    /// <summary>
    /// Returns a list of all the cards
    /// </summary>
    public List<UpgradeDataSO> GetAllCards()
    {
        return allCards;
    }
}