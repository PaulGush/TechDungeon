using Input;
using ObjectPool;
using UnityEngine;
using UnityEngine.InputSystem;

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
            projectile.transform.position = _shootPoint.position;
            
            Vector3 diff = _camera.ScreenToWorldPoint(InputSystem.GetDevice<Mouse>().position.ReadValue()) - transform.position;
            diff.Normalize();  
            float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            projectile.transform.rotation =  Quaternion.Euler(0f, 0f, rotZ);
            projectile.GetComponent<Projectile>().Move();
        }
    }
}
