using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class EnemyShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController m_enemyController;
    [SerializeField] private Transform m_shootPoint;
    [SerializeField] private EnemySettings m_settings;

    private EnemyAnimationController m_animationController;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_projectilePrefab;

    private ObjectPool m_pool;
    private float m_lastTimeFired;

    private void Awake()
    {
        m_animationController = m_enemyController.AnimationController;
        if (ServiceLocator.Global.TryGet(out ObjectPool pool))
        {
            m_pool = pool;
        }
    }

    public void TryShoot()
    {
        if (!(m_lastTimeFired + m_settings.FireRate <= Time.time))
            return;

        m_animationController.OnAttack();
    }

    public void Shoot()
    {
        GameObject projectile = m_pool.GetPooledObject(m_projectilePrefab);
        projectile.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
        m_lastTimeFired = Time.time;
    }
}
