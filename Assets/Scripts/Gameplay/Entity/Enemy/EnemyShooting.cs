using ObjectPool;
using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private EnemyController _enemyController;
    [SerializeField] private Transform _shootPoint;
    
    private EnemyAnimationController _animationController;
    private EnemyTargeting _targeting;
    
    [Header("Settings")]
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private GameObject _projectilePrefab;


    private float _shootPointStartingXPosition;
    private float _lastTimeFired;

    private void Awake()
    {
        _animationController = _enemyController.AnimationController;
        _targeting = _enemyController.Targeting;
        _shootPointStartingXPosition = _shootPoint.localPosition.x;
    }

    public void TryShoot()
    {
        if (!(_lastTimeFired + _fireRate <= Time.time))
            return;

        _animationController.OnAttack();
    }

    public void Shoot()
    {
        if (!_targeting.IsTargetRightOfTransform())
        {
            SetShootPointLeft();
        }
        else
        {
            SetShootPointRight();
        }

        Quaternion rotation = Quaternion.identity;
        if (_targeting.CurrentTarget != null)
        {
            Vector3 direction = _targeting.CurrentTarget.position - _shootPoint.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rotation = Quaternion.Euler(0, 0, angle);
        }
        
        GameObject projectile = SimplePool.Instance.GetPooledObject(_projectilePrefab);
        projectile.transform.SetPositionAndRotation(_shootPoint.position, rotation);
        projectile.GetComponent<Projectile>().Move();
        _lastTimeFired = Time.time;
    }

    private void SetShootPointLeft()
    {
        _shootPoint.transform.localPosition = new Vector3(-_shootPointStartingXPosition, _shootPoint.localPosition.y, _shootPoint.localPosition.z);
    }

    private void SetShootPointRight()
    {
        _shootPoint.transform.localPosition = new Vector3(_shootPointStartingXPosition, _shootPoint.localPosition.y, _shootPoint.localPosition.z);
    }
}