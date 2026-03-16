using Gameplay.ObjectPool;
using Input;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class WeaponShooting : MonoBehaviour, IWeapon
{
    [Header("References")]
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private Transform m_shootPoint;

    [Header("Prefabs")]
    [SerializeField] private Projectile m_projectile;

    private ObjectPool m_pool;
    private Transform m_weaponHolder;
    private bool m_isObstructed;

    public bool IsObstructed => m_isObstructed;
    public Vector2 ShootPointPosition => m_shootPoint.position;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
    }

    public void Equip()
    {
        m_weaponHolder = GetComponentInParent<WeaponHolder>().transform;
        m_inputReader.Attack += OnAttack;
    }

    public void Unequip()
    {
        m_inputReader.Attack -= OnAttack;
        m_isObstructed = false;
    }

    private void Update()
    {
        if (m_weaponHolder == null) return;

        Vector2 origin = m_weaponHolder.position;
        Vector2 target = m_shootPoint.position;
        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        m_isObstructed = Physics2D.Raycast(origin, direction, distance, GameConstants.Layers.WallsLayerMask);
    }

    private void OnAttack()
    {
        if (m_pool == null || m_isObstructed) return;

        GameObject projectile = m_pool.GetPooledObject(m_projectile.gameObject);
        projectile.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);
        projectile.GetComponent<Projectile>().Initialize();
    }
}
