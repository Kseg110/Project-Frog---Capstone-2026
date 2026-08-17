using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UINavigator : MonoBehaviour
{
    [Header("Selection Visuals")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Default Selection")]
    [SerializeField] private Selectable defaultButton;

    [SerializeField] private FMODUnity.EventReference hoverEvent;

    private GameObject lastSelected;
    private bool hasInitializedDefault = false;
    private GameObject lastControllerSelected;

    void OnEnable()
    {
        if (!hasInitializedDefault)
        {
            // StartCoroutine to select the first button after a short delay
            StartCoroutine(WaitAndSelectFirstButton());
            hasInitializedDefault = true;
        }
    }
    IEnumerator WaitAndSelectFirstButton()
    {
        yield return null; // wait one frame for the UI to initialize
        yield return new WaitForSeconds(0.05f); // small delay to ensure the UI is ready

        Selectable first = defaultButton != null ? defaultButton : GetComponentInChildren<Selectable>();
       
        if (first != null)
        {
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            lastSelected = first.gameObject;
            lastSelected.transform.localScale = Vector3.one * selectedScale;
        }
    }

    void Update()
    {
        HandleSelectionVisual();
    }

    private void HandleSelectionVisual()
    {
        var es = EventSystem.current;
        GameObject current = null;

        if (UIHoverScaler.HoveredObject != null)
        {
            current = UIHoverScaler.HoveredObject;
        }
        else if (es.currentSelectedGameObject != null)
        {
            current = es.currentSelectedGameObject;
        }
        else
        {
            current = lastSelected;
        }

        if (current == null)
            return;

        Slider parentSlider = current.GetComponentInParent<Slider>();
        if (parentSlider != null)
            current = parentSlider.gameObject;

        if (current != lastSelected)
        {
            if (lastSelected != null)
                lastSelected.transform.localScale = Vector3.one;

            current.transform.localScale = Vector3.one * selectedScale;

            FMODUnity.RuntimeManager.PlayOneShot(hoverEvent);

            lastSelected = current;
        }
    }
}
