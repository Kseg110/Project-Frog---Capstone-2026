using System.Collections.Generic;
using UnityEngine;

public class CardPoolManager : MonoBehaviour
{
    [Header("Card Pool")]
    [SerializeField] private List<CardData> allCards;

    private List<CardData> availableCards = new List<CardData>();

    [Header("Rarity Chances")]
    [Range(0, 100)] public int commonChance = 90;
    [Range(0, 100)] public int rareChance = 10;

    private void Awake()
    {
        // Initialize pool
        availableCards = new List<CardData>(allCards);

        // Reset all card levels at the start of a run
        foreach (var card in availableCards)
            card.currentLevel = 0;
    }

    public List<CardData> GetRandomCards(int count)
    {
        List<CardData> result = new List<CardData>();

        // Temporary pool to avoid duplicates in the same draw
        List<CardData> tempPool = new List<CardData>(availableCards);

        for (int i = 0; i < count; i++)
        {
            if (tempPool.Count == 0)
                break;

            CardData card = DrawOneCard(tempPool);
            result.Add(card);
            tempPool.Remove(card);
        }

        return result;
    }

    private CardData DrawOneCard(List<CardData> pool)
    {
        // First: filter by rarity based on chance
        int roll = Random.Range(0, 100);

        CardRarity targetRarity = (roll < rareChance) ? CardRarity.Rare : CardRarity.Common;

        // Try to get cards of that rarity
        List<CardData> rarityPool = pool.FindAll(c => c.rarity == targetRarity && !c.IsMaxed);

        // If none available, fallback to any card
        if (rarityPool.Count == 0)
            rarityPool = pool.FindAll(c => !c.IsMaxed);

        // Final fallback (should never happen)
        if (rarityPool.Count == 0)
            return pool[Random.Range(0, pool.Count)];

        return rarityPool[Random.Range(0, rarityPool.Count)];
    }

    public void OnCardChosen(CardData card)
    {
        // Increase level
        card.currentLevel++;

        // If maxed OR only 1 level → remove from pool
        if (card.IsMaxed)
            availableCards.Remove(card);
    }
}