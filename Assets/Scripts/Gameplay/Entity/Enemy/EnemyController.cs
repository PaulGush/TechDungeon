using UnityEngine;

public class EnemyController : Entity
{
    private EnemyStateMachine _stateMachine;
    public EnemyStateMachine StateMachine => _stateMachine;

    [Header("Dependencies")] 
    [SerializeField] private EnemyAnimationController _animationController;
    public EnemyAnimationController AnimationController => _animationController;
    
    [SerializeField] private EntityHealth _health;
    public EntityHealth Health => _health;
    
    [SerializeField] private EnemyMovement _movement;
    public EnemyMovement Movement => _movement;

    [SerializeField] private EnemyShooting _shooting;
    public EnemyShooting Shooting => _shooting;
    
    [SerializeField] private EnemyTargeting _targeting;
    public EnemyTargeting Targeting => _targeting;
    

    private void Start()
    {
        _stateMachine = new EnemyStateMachine(this);
        _stateMachine?.Initialize();
    }

    private void Update()
    {
        _stateMachine?.Tick();
    }
}