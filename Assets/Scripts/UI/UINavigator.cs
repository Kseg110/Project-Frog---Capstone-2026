using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UINavigator : MonoBehaviour
{
    [Header("Selection Visuals")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float scaleSpeed = 10f;

    private GameObject lastSelected;

    void OnEnable()
    {
        // StartCoroutine to select the first button after a short delay
        StartCoroutine(WaitAndSelectFirstButton());
    }
    IEnumerator WaitAndSelectFirstButton()
    {
        yield return null; // wait one frame for the UI to initialize
        yield return new WaitForSeconds(0.05f); // snall delay to ensure the UI is ready

        Selectable first = GetComponentInChildren<Selectable>();
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    void Update()
    {
        HandleSelectionVisual();
    }

    private void HandleSelectionVisual()
    {
        var es = EventSystem.current;
        GameObject current = es.currentSelectedGameObject;

        if (current == lastSelected)
            return;

        if (lastSelected != null)
            lastSelected.transform.localScale = Vector3.one;

        if (current != null)
            current.transform.localScale = Vector3.one * selectedScale;

        lastSelected = current;
    }
}