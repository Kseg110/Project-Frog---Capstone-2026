using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MovementComponent))]
public class DashComponent : MonoBehaviour
{
    private MovementComponent movement;
    private NavMeshAgent agent;

    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float rechargeTime = 2f;
    [SerializeField] private int maxDashes = 1;

    private int currentDashes; //how many dashe the enemy has stored//'
    private bool isRecharging;
    private bool isDashing;


    //preserve the enemy's movement before dashing//
    private float originalSpeed; 
    private float originalAcceleration;

    private void Awake()
    {
        movement = GetComponent<MovementComponent>();
        agent = movement.Agent;
    }

    private void Start()
    {
        currentDashes = maxDashes;

        originalSpeed = agent.speed;
        originalAcceleration = agent.acceleration;
    }

    public bool CanDash()
    {
        return !isDashing && currentDashes > 0;
    }
    public IEnumerator Dash(Vector3 direction)
    {
        if (!CanDash()) yield break;

        isDashing = true;
        currentDashes--;

        if (!isRecharging) StartCoroutine(RechargeDash());

        Vector3 dashTarget = transform.position + direction.normalized * dashDistance;

        NavMeshHit hit;

        if(NavMesh.SamplePosition(dashTarget, out hit, 2f, NavMesh.AllAreas))
        {
            dashTarget = hit.position;
        }

        agent.speed = dashSpeed;
        agent.acceleration = 999f;

        movement.MoveTo(dashTarget);

        yield return new WaitForSeconds(dashDuration);

        agent.speed = originalSpeed;
        agent.acceleration = originalAcceleration;

        isDashing = false;
    }

    private IEnumerator RechargeDash()
    {
        isRecharging = true;

        while(currentDashes < maxDashes)
        {
            yield return new WaitForSeconds(rechargeTime); 

            currentDashes++;
        }

        isRecharging = false;   
    }

}
