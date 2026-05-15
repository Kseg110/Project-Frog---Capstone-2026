using UnityEngine;

public class AttackState : BossStates
{
    public AttackState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Movement.Stop();
    }

    public override void Update()
    {
        if (stateMachine.Target == null) return;

        float distance = stateMachine.DistanceToPlayer();

        //if no attck can reach return to chase state//
        if (!stateMachine.Combat.HasAnyAttackInRange(distance))
        {
            stateMachine.ChangeState(stateMachine.ChaseState);
            return;
        }

        FaceTarget();

        var attack = stateMachine.Combat.GetAvailableAttack(distance);

        if(attack != null )
        {
            stateMachine.Combat.ExecuteAttack(attack);
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = (stateMachine.Target.position - stateMachine.transform.position).normalized;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        stateMachine.transform.rotation = Quaternion.Slerp(
            stateMachine.transform.rotation,
            lookRotation,
            Time.deltaTime * 10f
        );
    }
}
