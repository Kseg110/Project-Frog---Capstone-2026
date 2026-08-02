using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] enemyRenderers;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Material customFlashMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> flashMaterials = new Dictionary<Renderer, Material[]>();

    private Material generatedFlashMaterial;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (enemyRenderers == null || enemyRenderers.Length == 0)
        {
            enemyRenderers = GetComponentsInChildren<Renderer>();
        }

        CacheOriginalColors();
    }

    private void CacheOriginalColors()
    {
        originalMaterials.Clear();
        flashMaterials.Clear();

        Material activeFlashMat = customFlashMaterial;
        if (activeFlashMat == null)
        {
            Shader flashShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (flashShader == null) flashShader = Shader.Find("Unlit/Color");
            if (flashShader == null) flashShader = Shader.Find("Sprites/Default");

            generatedFlashMaterial = new Material(flashShader);

            if (generatedFlashMaterial.HasProperty("_BaseColor"))
            {
                generatedFlashMaterial.SetColor("_BaseColor", flashColor);
            }
            if (generatedFlashMaterial.HasProperty("_Color"))
            {
                generatedFlashMaterial.SetColor("_Color", flashColor);
            }

            activeFlashMat = generatedFlashMaterial;
        }

        foreach (Renderer rend in enemyRenderers)
        {
            if (rend == null) continue;
            if (rend.GetComponentInParent<Canvas>() != null) continue;

            Material[] origMats = rend.sharedMaterials;
            originalMaterials[rend] = origMats;

            Material[] fMats = new Material[origMats.Length];
            for (int i = 0; i < origMats.Length; i++)
            {
                fMats[i] = activeFlashMat;
            }

            flashMaterials[rend] = fMats;
        }
    }

    public void Flash()
    {
        if (originalMaterials.Count == 0) return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        foreach (var kvp in flashMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }

        flashCoroutine = null;
    }

    private void OnDestroy()
    {
        if (generatedFlashMaterial != null)
        {
            Destroy(generatedFlashMaterial);
        }
    }
}
