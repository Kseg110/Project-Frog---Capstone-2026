using Unity.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDmg(float dmg);

    // overload for status effects
    void TakeDmg(float dmg, string effectTytpe, float effectDuration, float effectValue);
}

///attach this to anything that can take damage so damage will be dealt regardless of what this is attached to
