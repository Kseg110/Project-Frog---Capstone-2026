using UnityEngine;

public class DebugController : MonoBehaviour
{

    [Header("Panels")]
    public GameObject panelGeneral;
    public GameObject panelPlayer;
    public GameObject panelEnemies;
    public GameObject panelGameMode;

    public GameObject panelRoot;

    private void Awake()
    {
        panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
            panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public void ShowGeneral() => ShowPanel(panelGeneral);
    public void ShowPlayer() => ShowPanel(panelPlayer);
    public void ShowEnemies() => ShowPanel(panelEnemies);
    public void ShowGameMode() => ShowPanel(panelGameMode);

    public void ShowPanel(GameObject target)
    {
        panelGeneral.SetActive(false);
        panelPlayer.SetActive(false);
        panelEnemies.SetActive(false);
        panelGameMode.SetActive(false);

        target.SetActive(true);
    }
}

