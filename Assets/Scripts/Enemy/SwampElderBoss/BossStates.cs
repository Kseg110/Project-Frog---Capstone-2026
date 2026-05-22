using UnityEngine;

public abstract class BossStates
{
    protected BossStateMachine stateMachine;

    public BossStates(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
