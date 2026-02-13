using System.Collections.Generic;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnWaves waveSpawner;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardUIPrefab;
    [SerializeField] private CardPoolManager cardPool;

    private void Start()
    {
        // Hide UI at start
        gameObject.SetActive(false);

        // Register callback from wave spawner
        waveSpawner.onWaveCompleted += ShowCardSelection;
    }

    private void ShowCardSelection()
    {
        // Pause game
        Time.timeScale = 0f;

        // Activate UI
        gameObject.SetActive(true);

        // Ask the pool for 3 valid cards
        List<CardData> selectedCards = cardPool.GetRandomCards(3);

        foreach (CardData card in selectedCards)
        {
            CardUI ui = Instantiate(cardUIPrefab, cardContainer);
            ui.Setup(card, OnCardChosen);
        }
    }

    private void OnCardChosen(CardData chosenCard)
    {
        // Tell the pool that this card was selected
        cardPool.OnCardChosen(chosenCard);

        // Destroy all card UI objects
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // Hide UI
        gameObject.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        // Start next wave
        waveSpawner.SpawnNextWave();
    }
}
