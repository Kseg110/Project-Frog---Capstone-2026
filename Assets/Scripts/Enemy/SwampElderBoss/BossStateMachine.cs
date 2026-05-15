using UnityEngine;
using UnityEngine.XR;

public class BossStateMachine : MonoBehaviour
{
    private BossStates currentState;

    //other component references//
    [SerializeField] private MovementComponent movement;
    [SerializeField] private CombatComponent combat;
    [SerializeField] private Transform target;


    public CombatComponent Combat => combat;
    public Transform Target => target;
    public MovementComponent Movement => movement;

    //cached states//
    public IdleState IdleState {  get; private set; }
    public ChaseState ChaseState { get; private set; }
    public AttackState AttackState { get; private set; }
    private void Awake()
    {
        if(movement == null)
             movement = GetComponent<MovementComponent>();
        if(combat == null)
            combat = GetComponent<CombatComponent>();

        //instantiate states //
        IdleState = new IdleState(this);
        ChaseState = new ChaseState(this);
        AttackState = new AttackState(this); 
    }

    private void Start()
    {
        movement.SetTarget(target);

        ChangeState(new IdleState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(BossStates newState)
    {
        currentState?.Exit();

        currentState = newState;

        currentState.Enter();
    }

    public float DistanceToPlayer()
    {
        if (target == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, target.position);
    }
}
