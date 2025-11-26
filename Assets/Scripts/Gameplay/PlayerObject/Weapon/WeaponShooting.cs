using Input;
using ObjectPool;
using UnityEngine;

public class WeaponShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private Camera _camera;
    
    [Header("Prefabs")]
    [SerializeField] private Projectile _bulletPrefab;

    private void OnEnable()
    {
        _inputReader.EnablePlayerActions();
        _inputReader.Attack += OnAttack;
    }

    private void OnDisable()
    {
        _inputReader.Attack -= OnAttack;
    }

    private void OnAttack(bool state)
    {
        if (state)
        {
            GameObject projectile = SimplePool.Instance.GetPooledObject(_bulletPrefab.gameObject);
            projectile.transform.SetPositionAndRotation(_shootPoint.position, _shootPoint.rotation);
            projectile.GetComponent<Projectile>().Move();
        }
    }
}
