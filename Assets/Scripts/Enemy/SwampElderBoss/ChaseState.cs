using UnityEngine;

public class ChaseState : BossStates
{
    public ChaseState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Movement.SetMovementEnabled(true);
    }
    
    public override void Update()
    {
        if (stateMachine.Target == null) return;

        float distance = stateMachine.DistanceToPlayer();

        //switch to attack state if in range//
        if(stateMachine.Combat.HasAnyAttackInRange(distance))
        {
            stateMachine.ChangeState(stateMachine.AttackState);
            return;
        }
    }

    public override void Exit()
    {
        stateMachine.Movement.Stop();
    }
}
