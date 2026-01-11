using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class FullScreen : MonoBehaviour
{
    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Start()
    {
        FitToScreen();
    }

    void OnRectTransformDimensionsChange()
    {
        FitToScreen();
    }

    private void FitToScreen()
    {
        // Get Screen ratio
        float screenRatio = (float)Screen.width / Screen.height;

        // Get Image origin size ratio
        Image img = GetComponent<Image>();
        RawImage raw = GetComponent<RawImage>();

        float imageRatio = 1f;

        if (img && img.sprite)
            imageRatio = (float)img.sprite.texture.width / img.sprite.texture.height;

        if (raw && raw.texture)
            imageRatio = (float)raw.texture.width / raw.texture.height;

        // Stretch total (anchors)
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        // crop to fit
        if (screenRatio > imageRatio)
        {
            // Larger screen ? augment width
            float height = rt.rect.height;
            float width = height * screenRatio;
            rt.sizeDelta = new Vector2(width - rt.rect.width, 0);
        }
        else
        {
            // Taller screen ? augment height
            float width = rt.rect.width;
            float height = width / screenRatio;
            rt.sizeDelta = new Vector2(0, height - rt.rect.height);
        }
    }
}

