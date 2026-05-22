using UnityEngine;

public class IdleState : BossStates
{
    public IdleState(BossStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Movement.Stop();
    }

    public override void Update()
    {
        if (stateMachine.Target == null) return;

        //if player exists, chase him//
        stateMachine.ChangeState(stateMachine.ChaseState);
    }
}
