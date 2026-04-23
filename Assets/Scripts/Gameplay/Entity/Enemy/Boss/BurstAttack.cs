using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class BurstAttack : MonoBehaviour
{
    [SerializeField] private Transform m_origin;
    [SerializeField] private GameObject m_projectilePrefab;

    private ObjectPool m_pool;

    private void Awake()
    {
        ServiceLocator.Global.TryGet(out m_pool);
    }

    public void Fire(int projectileCount)
    {
        if (m_pool == null || m_projectilePrefab == null) return;

        float angleStep = 360f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angleStep * i);
            ProjectileSpawner.Spawn(m_pool, m_projectilePrefab, m_origin.position, rotation);
        }
    }
}
