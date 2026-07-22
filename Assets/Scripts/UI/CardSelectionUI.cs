using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

public class CardSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveRoundSystem waveSpawner;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardUIPrefab;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private UIPlayerHUD playerHUD;
    [SerializeField] private PlayerCrosshair playerCrosshair;

    private CanvasGroup canvasGroup;
    public bool IsCardSelectionActive { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        upgradeManager = UpgradeManager.Instance;
    }

    private void Start()
    {
        // Card selection is hidden when the game starts
        HideUI();
    }

    public void ShowCardSelectionFromWave()
    {
        ShowCardSelection();
    }

    private void ShowUI()
    {
        // Make the card selection screen fully visible and responsive to clicks
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideUI()
    {
        // Make the card selection screen invisible and ignore all clicks
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowCardSelection()
    {
        IsCardSelectionActive = true;
        Time.timeScale = 0f;
        ShowUI();
        playerHUD.HideHUD();
        Cursor.visible = true;

        // Hide the player's crosshair while selecting a card
        if (playerCrosshair != null)
            playerCrosshair.gameObject.SetActive(false);

        var manager = UpgradeManager.Instance;
       
        // Ask the upgrade manager to pick 3 random cards for the player to choose from
        List<UpgradeDataSO> selectedCards = upgradeManager.GetRandomCards(3);
        StartCoroutine(SpawnCardsSequentially(selectedCards));
    }

    private IEnumerator SpawnCardsSequentially(List<UpgradeDataSO> cards)
    {
        // Spawn each card one at a time with a short delay between them for a staggered animation effect
        // WaitForSecondsRealtime is used here because normal timers stop while the game is frozen
        foreach (UpgradeDataSO card in cards)
        {
            CardUI ui = Instantiate(cardUIPrefab, cardContainer);
            ui.Setup(card, OnCardChosen);
            ui.PlaySpawnAnimation();
            yield return new WaitForSecondsRealtime(0.35f);
        }

        // After all cards are spawned, set the first card as the selected button for controller/keyboard navigation
        yield return null; // wait one frame for the EventSystem to recognize the buttons
        var firstButton = cardContainer.GetComponentInChildren<UnityEngine.UI.Selectable>();
        if (firstButton != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }


    private void OnCardChosen(UpgradeDataSO chosenCard)
    {
        IsCardSelectionActive = false;

        // Tell the upgrade manager which card the player picked so it can apply the upgrade
        upgradeManager.OnCardChosen(chosenCard);

        // Remove all card UI objects from the screen
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // Hide the selection screen, unfreeze the game, and lock the cursor again
        HideUI();

        // Unfreeze game
        Time.timeScale = 1f;

        playerHUD.ShowHUD();
        Cursor.visible = false;

        // Show the player's crosshair again now that card selection is over
        if (playerCrosshair != null)
            playerCrosshair.gameObject.SetActive(true);

        //call next wave
        waveSpawner.StartNextWaveAfterCard();
    }
}