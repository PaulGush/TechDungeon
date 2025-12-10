using Gameplay.ObjectPool;
using Input;
using UnityEngine;
using UnityServiceLocator;

public class WeaponShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private Camera _camera;
    
    [Header("Prefabs")]
    [SerializeField] private Projectile _projectile;

    private ObjectPool _pool;
    
    private void Awake()
    {
        ServiceLocator.Global.Get(out ObjectPool simplePool);
        _pool = simplePool;
    }

    private void OnEnable()
    {
        _inputReader.EnablePlayerActions();
        _inputReader.Attack += OnAttack;
    }

    private void OnDisable()
    {
        _inputReader.Attack -= OnAttack;
    }

    private void OnAttack()
    {
        GameObject projectile = _pool.GetPooledObject(_projectile.gameObject);
        projectile.transform.SetPositionAndRotation(_shootPoint.position, _shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
    }
}