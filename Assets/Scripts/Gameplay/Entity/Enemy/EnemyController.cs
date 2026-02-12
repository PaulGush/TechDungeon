using UnityEngine;

public class EnemyController : Entity
{
    private EnemyStateMachine m_stateMachine;
    public EnemyStateMachine StateMachine => m_stateMachine;

    [Header("Dependencies")] 
    [SerializeField] private EnemyAnimationController m_animationController;
    public EnemyAnimationController AnimationController => m_animationController;
    
    [SerializeField] private EntityHealth m_health;
    public EntityHealth Health => m_health;
    
    [SerializeField] private EnemyMovement m_movement;
    public EnemyMovement Movement => m_movement;

    [SerializeField] private EnemyShooting m_shooting;
    public EnemyShooting Shooting => m_shooting;
    
    [SerializeField] private EnemyTargeting m_targeting;
    public EnemyTargeting Targeting => m_targeting;
    

    private void Start()
    {
        m_stateMachine = new EnemyStateMachine(this);
        m_stateMachine?.Initialize();
    }

    private void FixedUpdate()
    {
        m_stateMachine?.Tick();
    }
}