using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

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

    private ObjectPool m_pool;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
    }

    private void OnEnable()
    {
        m_health.ResetHealth();
        m_stateMachine = new EnemyStateMachine(this);
        m_stateMachine.Initialize();
    }

    private void FixedUpdate()
    {
        m_stateMachine?.Tick();
    }

    public void ReturnToPool()
    {
        if (m_pool != null)
        {
            m_pool.ReturnGameObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
