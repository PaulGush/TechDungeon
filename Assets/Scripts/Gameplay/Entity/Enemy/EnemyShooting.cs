using ObjectPool;
using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private Transform _shootPoint;
    
    private EnemyAnimationController _animationController;
    
    [Header("Settings")]
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private GameObject _projectilePrefab;
    
    private float _lastTimeFired;

    private void Awake()
    {
        _animationController = _enemyController.AnimationController;
    }

    public void TryShoot()
    {
        if (!(_lastTimeFired + _fireRate <= Time.time))
            return;

        _animationController.OnAttack();
    }

    public void Shoot()
    {
        GameObject projectile = SimplePool.Instance.GetPooledObject(_projectilePrefab);
        projectile.transform.SetPositionAndRotation(_shootPoint.position, _shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
        _lastTimeFired = Time.time;
    }
}