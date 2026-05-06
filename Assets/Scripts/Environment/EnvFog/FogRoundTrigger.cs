using UnityEngine;

public class FogRoundTrigger : MonoBehaviour
{
    public FogEnvRoundSystem system;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        system.ActivateFromTrigger(GetComponent<Collider>());
    }
}