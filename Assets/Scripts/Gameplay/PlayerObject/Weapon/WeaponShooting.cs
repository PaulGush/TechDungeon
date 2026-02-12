using Gameplay.ObjectPool;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class WeaponShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private Transform m_shootPoint;
    
    [Header("Prefabs")]
    [SerializeField] private Projectile m_projectile;
    
    private ObjectPool m_pool;
    
    private void Start()
    {
        ServiceLocator.Global.Get(out ObjectPool simplePool);
        m_pool = simplePool;
    }

    private void OnEnable()
    {
        m_inputReader.EnablePlayerActions();
        m_inputReader.Attack += OnAttack;
    }

    private void OnDisable()
    {
        m_inputReader.Attack -= OnAttack;
    }

    private void OnAttack()
    {
        GameObject projectile = m_pool.GetPooledObject(m_projectile.gameObject);
        projectile.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
    }
}