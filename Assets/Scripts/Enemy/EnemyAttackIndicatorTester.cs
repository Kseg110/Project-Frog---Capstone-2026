using UnityEngine;

public class EnemyAttackIndicatorTester : MonoBehaviour
{
    public EnemyAttackIndicator attackIndicator;

    public float repeatEvery = 3f;

    private float timer;
    
    private void Start()
    {
        if (attackIndicator == null)
        {
            attackIndicator = GetComponent<EnemyAttackIndicator>();
        }
    }
    //repeats attack
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= repeatEvery)
        {
            timer = 0f;

            attackIndicator.TriggerAttackIndicator();
        }
    }
}