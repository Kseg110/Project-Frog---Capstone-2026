using UnityEngine;

[System.Serializable]
public class BossAttackData
{
    [SerializeField] private float range = 3f;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private string attackName;

    [HideInInspector] public float cooldownTimer;



    public float Range => range;
    public float Cooldown => cooldown;
    public string AttackName => attackName;
}
