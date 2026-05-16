using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CombatComponent : MonoBehaviour
{
    [Header("Attacks")]
    [SerializeField] private AttackBaseSO[] attacks;

    [SerializeField] private Transform player;

    private void Update()
    {
    }

    //check which attack can currently be used//
    public AttackBaseSO GetAvailableAttack(float distanceToTarget)
    {
        List<AttackBaseSO> validAttacks = new List<AttackBaseSO>();

        foreach(var attack in attacks)
        {
            attack.CanAttack(player, transform);
        }
        if (validAttacks.Count == 0)
            return null;

        return validAttacks[Random.Range(0, validAttacks.Count)]; 
    }

    public bool HasAnyAttackInRange(float distanceToTarget)
    {
        foreach(AttackBaseSO attack in attacks)
        {
           if (attack.CanAttack(player, transform))
           return true;
        }
        return false;
    }

    //Execute attack (CURRENTLY PLACEHOLDER, ADD ATTACK FUCTIONS LATER)//
    public void ExecuteAttack(AttackBaseSO attack)
    {
        if (attack == null) return;

        attack.Attack(player, transform);

        //reset cooldown//
        attack.lastUsed = Time.time;

        //add animations//
        //add hitbox//

    }

}
