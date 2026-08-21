using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPanelDefault : MonoBehaviour
{
    [SerializeField] private Selectable defaultSelectable;

    void OnEnable()
    {
        if (defaultSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectable.gameObject);
        }
    }
}