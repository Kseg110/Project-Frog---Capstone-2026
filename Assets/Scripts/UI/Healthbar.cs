using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class Healthbar: MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    [SerializeField] private Transform uiContainer;

    private Camera mainCamera;
    private Canvas canvas;

    public void UpdateHealthBar(float maxHealth, float curHealth)
    {
        // Change the fill amount of the foreground to the percentage of health left
        foregroundImage.fillAmount = curHealth / maxHealth;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        canvas = GetComponent<Canvas>();

        Debug.Assert(foregroundImage != null, $"[{gameObject.name}] foregroundImage is not assigned!", this);
        Debug.Assert(uiContainer != null, $"[{gameObject.name}] uiContainer is not assigned!", this);
    }

    private void Start()
    {
        canvas.worldCamera = mainCamera;
    }

    private void LateUpdate()
    {
        // Point the healthbar to the cameraa
        uiContainer.forward = mainCamera.transform.forward;
    }
}