using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static GameObject HoveredObject;

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoveredObject = gameObject;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoveredObject == gameObject)
            HoveredObject = null;
    }
}