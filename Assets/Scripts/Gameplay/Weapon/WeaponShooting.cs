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

    [Header("Feedback")]
    [Tooltip("Impulse amplitude applied to the camera shake service each time the weapon fires. Keep this subtle — shoot shake should be tactile, not disorienting. Zero disables.")]
    [SerializeField] private float m_shootShakeAmplitude = 0.02f;

    [Tooltip("Pooled muzzle flash prefab spawned at the shoot point each time the weapon fires. Leave empty to skip.")]
    [SerializeField] private GameObject m_muzzleFlashPrefab;

    private ObjectPool m_pool;
    private Transform m_weaponHolder;
    private MutationManager m_mutationManager;
    private AmmoManager m_ammoManager;
    private CameraShake m_cameraShake;

    public Vector2 ShootPointPosition => m_shootPoint.position;

    private void Start()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
        ServiceLocator.Global.TryGet(out m_mutationManager);
        ServiceLocator.Global.TryGet(out m_ammoManager);
        ServiceLocator.Global.TryGet(out m_cameraShake);
    }

    public void Equip()
    {
        m_weaponHolder = GetComponentInParent<WeaponHolder>().transform;
        m_inputReader.Attack += OnAttack;
    }

    public void Unequip()
    {
        m_inputReader.Attack -= OnAttack;
    }

    private bool IsShootPointObstructed()
    {
        if (m_weaponHolder == null) return false;

        Vector2 origin = m_weaponHolder.position;
        Vector2 target = m_shootPoint.position;
        Vector2 direction = target - origin;

        return Physics2D.Raycast(origin, direction, direction.magnitude, GameConstants.Layers.WallsLayerMask);
    }

    private void OnAttack()
    {
        if (m_pool == null || IsShootPointObstructed()) return;

        // Resolve ammo type
        AmmoSettings ammoSettings = null;
        if (m_ammoManager != null)
        {
            AmmoSettings current = m_ammoManager.CurrentAmmoSettings;
            if (current != null && current.Type != AmmoType.Standard)
            {
                if (m_ammoManager.TryConsumeAmmo())
                    ammoSettings = current;
            }
        }

        GameObject projectile = m_pool.GetPooledObject(m_projectile.gameObject);
        projectile.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);

        Projectile proj = projectile.GetComponent<Projectile>();
        proj.SetProjectilePrefab(m_projectile.gameObject);

        if (m_mutationManager != null)
        {
            proj.SetMutationModifiers(
                m_mutationManager.GetFlatDamageBonus(),
                m_mutationManager.GetDamageMultiplier(),
                m_mutationManager.GetBonusPierce()
            );
        }

        if (ammoSettings != null)
        {
            proj.SetAmmoModifiers(ammoSettings);
        }

        proj.Initialize();

        if (m_muzzleFlashPrefab != null)
        {
            GameObject flash = m_pool.GetPooledObject(m_muzzleFlashPrefab);
            flash.transform.SetPositionAndRotation(m_shootPoint.position, m_shootPoint.rotation);
        }

        if (m_cameraShake != null && m_shootShakeAmplitude > 0f)
            m_cameraShake.Shake(m_shootShakeAmplitude, -m_shootPoint.right);
    }
}
