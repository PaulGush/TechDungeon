using Input;
using ObjectPool;
using UnityEngine;

public class WeaponShooting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _shootPoint;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject _bulletPrefab;

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
            GameObject projectile = SimplePool.Instance.GetPooledObject(_bulletPrefab);
            projectile.transform.position = _shootPoint.position;
        }
    }
}
