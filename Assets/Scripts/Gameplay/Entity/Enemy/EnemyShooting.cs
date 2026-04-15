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

        GameObject projectile = m_pool.GetPooledObject(m_projectilePrefab);
        projectile.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
        m_lastTimeFired = Time.time;
    }
}
