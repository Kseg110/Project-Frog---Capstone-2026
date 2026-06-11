using UnityEngine;
using UnityEngine.EventSystems;
using FMODUnity;

public class UIButtonSoundManager : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("FMod Events")]
    [SerializeField] private EventReference hoverEvent;
    [SerializeField] private EventReference clickEvent;

    public void OnPointerEnter(PointerEventData eventData)
    {
        RuntimeManager.PlayOneShot(hoverEvent, transform.position);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RuntimeManager.PlayOneShot(clickEvent, transform.position);
    }
}
