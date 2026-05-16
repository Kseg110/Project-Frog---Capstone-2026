using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CombatComponent : MonoBehaviour
{
    [Header("Attacks")]
    [SerializeField] private BossAttackData[] attacks;

    private void Update()
    {
        foreach (var attack in attacks)
        {
            if (attack.cooldownTimer > 0f)
                attack.cooldownTimer -= Time.deltaTime;
        }
    }

    //check which attack can currently be used//
    public BossAttackData GetAvailableAttack(float distanceToTarget)
    {
        List<BossAttackData> validAttacks = new List<BossAttackData>();

        foreach(var attack in attacks)
        {
            if(distanceToTarget <= attack.Range && attack.cooldownTimer <= 0f)
            {
                validAttacks.Add(attack);
            }
        }
        if (validAttacks.Count == 0)
            return null;

        return validAttacks[Random.Range(0, validAttacks.Count)]; 
    }

    public bool HasAnyAttackInRange(float distanceToTarget)
    {
        foreach(var attack in attacks)
        {
            if (distanceToTarget <= attack.Range)
                return true;
        }
        return false;
    }

    //Execute attack (CURRENTLY PLACEHOLDER, ADD ATTACK FUCTIONS LATER)//
    public void ExecuteAttack(BossAttackData attack)
    {
        if (attack == null) return;

        Debug.Log("Boss used " + attack.AttackName);

        //reset cooldown//
        attack.cooldownTimer = attack.Cooldown;

        //add animations//
        //add hitbox//

    }

}
