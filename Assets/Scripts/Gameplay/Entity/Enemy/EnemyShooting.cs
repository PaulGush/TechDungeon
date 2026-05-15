using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class EnemyShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected EnemyController m_enemyController;
    [SerializeField] protected Transform m_shootPoint;
    [SerializeField] protected EnemySettings m_settings;

    protected EnemyAnimationController m_animationController;

    [Header("Prefabs")]
    [SerializeField] protected GameObject m_projectilePrefab;

    protected ObjectPool m_pool;
    protected float m_lastTimeFired;
    private int m_lastShotFrame = -1;

    public void SetRuntimeSettings(EnemySettings runtimeSettings)
    {
        m_settings = runtimeSettings;
    }

    protected virtual void Awake()
    {
        m_animationController = m_enemyController.AnimationController;
        if (ServiceLocator.Global.TryGet(out ObjectPool pool))
        {
            m_pool = pool;
        }
    }

    public virtual void TryShoot()
    {
        if (!(m_lastTimeFired + m_settings.FireRate <= Time.time))
            return;

        m_animationController.OnAttack();
    }

    public virtual void Shoot()
    {
        if (m_pool == null) return;

        // Guard against double-firing within a single frame. The 4-direction shoot BlendTree
        // (turrets) blends two clips when aiming between cardinal thresholds, and each clip's
        // animation event fires Shoot independently — so an off-axis shot would spawn two
        // projectiles per trigger and double the turret's effective DPS.
        if (Time.frameCount == m_lastShotFrame) return;
        m_lastShotFrame = Time.frameCount;

        ProjectileSpawner.Spawn(m_pool, m_projectilePrefab, m_shootPoint.position, m_shootPoint.rotation);
        m_lastTimeFired = Time.time;
    }
}
