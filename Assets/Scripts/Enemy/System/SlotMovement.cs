using UnityEngine;

public class SlotMovement : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float frequency = 2f;
    [SerializeField] private float randomPhase = 1f;

    private Vector3 baseLocalPosition;
    private Vector3 direction;
    private float phaseOffset;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        direction = baseLocalPosition.normalized;

        phaseOffset = Random.Range(0f, randomPhase * Mathf.PI * 2f);
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency + phaseOffset) * amplitude;
        transform.localPosition = baseLocalPosition + direction * offset;
    }
}
