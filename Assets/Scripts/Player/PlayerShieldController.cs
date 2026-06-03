using UnityEngine;
using System;

public class PlayerShieldController : MonoBehaviour
{
    public event Action OnShieldBroken;

    private bool hasShield = false;

    public void GiveShield()
    {
        hasShield = true;
        Debug.Log("Shield gained!");
    }

    public void RemoveShield()
    {
        hasShield = false;
        Debug.Log("Shield removed.");
    }

    public void TakeDamage(int dmg)
    {
        if (hasShield)
        {
            hasShield = false;
            Debug.Log("Shield broken!");
            OnShieldBroken?.Invoke();
            return;
        }

        // TODO: apply real damage to player
    }
}