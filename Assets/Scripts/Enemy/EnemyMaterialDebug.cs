using UnityEngine;

public class EnemyMaterialDebug : MonoBehaviour
{
    private Renderer[] renderers;
    private Material[] lastMaterials;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        lastMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            lastMaterials[i] = renderers[i].material;
        }
    }

    private void Update()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Material currentMaterial = renderers[i].material;

            if (currentMaterial != lastMaterials[i])
            {
                Debug.Log(
                    $"[{Time.time:F2}s] Material changed on {renderers[i].gameObject.name}: " +
                    $"{lastMaterials[i].name} -> {currentMaterial.name}",
                    renderers[i].gameObject
                );

                lastMaterials[i] = currentMaterial;
            }
        }
    }
}
