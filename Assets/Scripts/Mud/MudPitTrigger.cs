using UnityEngine;

public class MudPitTrigger : MonoBehaviour
{
    private MudPit mudpit;

    private void Awake()
    {
        mudpit = GetComponentInParent<MudPit>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"MudPitTrigger hit by: {other.name}, IMovement found: {other.GetComponentInParent<IMovement>() != null}");
        mudpit.HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        mudpit.HandleExit(other);
    }


}
