using UnityEngine;

public class SelfDestructFailsafe : MonoBehaviour
{
    // Sumary: Wake up and explode //
    [SerializeField] private float lifetime = 0.2f;

    private void Awake()
    {
        Destroy(gameObject, lifetime);
    }
}
