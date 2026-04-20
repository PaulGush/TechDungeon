using System;
using System.Collections;
using Gameplay.ObjectPool;
using Input;
using PlayerObject;
using UnityEngine;
using UnityServiceLocator;

public class WeaponShooting : MonoBehaviour, IWeapon
{
    public event Action<AmmoSettings> OnFired;

    [Header("References")]
    [SerializeField] private InputReader m_inputReader;
    [SerializeField] private Transform m_shootPoint;
    [SerializeField] private WeaponSettings m_settings;

    [Header("Prefabs")]
    [SerializeField] private Projectile m_projectile;

    [Header("Feedback")]
    [Tooltip("Impulse amplitude applied to the camera shake service each time the weapon fires. Keep this subtle — shoot shake should be tactile, not disorienting. Zero disables.")]
    [SerializeField] private float m_shootShakeAmplitude = 0.02f;

    [Tooltip("Child object on the weapon that is toggled on for a brief flash each time the weapon fires. Leave empty to skip.")]
    [SerializeField] private GameObject m_muzzleFlashObject;

    [Tooltip("Seconds the muzzle flash object stays active after firing.")]
    [SerializeField] private float m_muzzleFlashDuration = 0.05f;

    private ObjectPool m_pool;
    private Transform m_weaponHolder;
    private MutationManager m_mutationManager;
    private AmmoManager m_ammoManager;
    private CameraShake m_cameraShake;
    private Coroutine m_muzzleFlashRoutine;
    private SpriteRenderer m_muzzleFlashRenderer;
    private Color m_muzzleFlashDefaultTint = Color.white;

    private bool m_equipped;
    private float m_cooldownEndsAt;
    private Coroutine m_burstRoutine;
    private float m_chargeStartTime = -1f;

    private float m_kickbackTimeRemaining;
    private float m_kickbackDuration;
    private float m_kickbackPeak;

    public Vector2 ShootPointPosition => m_shootPoint.position;

    // Non-positive offset applied along the weapon's local -Y to visualize kickback.
    // WeaponHolder reads this each LateUpdate and layers it on top of the clamped/base position.
    public float CurrentKickbackOffset { get; private set; }

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
        m_equipped = true;
        m_cooldownEndsAt = 0f;
        m_chargeStartTime = -1f;

        if (m_settings == null)
            Debug.LogWarning($"WeaponShooting on '{name}' has no WeaponSettings assigned — falling back to unthrottled semi-auto.", this);

        if (m_muzzleFlashObject != null)
        {
            if (m_muzzleFlashRenderer == null)
            {
                m_muzzleFlashRenderer = m_muzzleFlashObject.GetComponentInChildren<SpriteRenderer>(true);
                if (m_muzzleFlashRenderer != null)
                    m_muzzleFlashDefaultTint = m_muzzleFlashRenderer.color;
            }
            m_muzzleFlashObject.SetActive(false);
        }
    }

    public void Unequip()
    {
        m_equipped = false;
        m_chargeStartTime = -1f;
        m_kickbackTimeRemaining = 0f;
        CurrentKickbackOffset = 0f;

        if (m_burstRoutine != null)
        {
            StopCoroutine(m_burstRoutine);
            m_burstRoutine = null;
        }

        if (m_muzzleFlashRoutine != null)
        {
            StopCoroutine(m_muzzleFlashRoutine);
            m_muzzleFlashRoutine = null;
        }
        if (m_muzzleFlashObject != null)
            m_muzzleFlashObject.SetActive(false);
    }

    private void Update()
    {
        TickKickback();

        if (!m_equipped || m_inputReader == null) return;

        bool held = m_inputReader.IsAttackHeld;
        WeaponFireMode mode = m_settings != null ? m_settings.FireMode : WeaponFireMode.SemiAuto;

        switch (mode)
        {
            case WeaponFireMode.SemiAuto:
            case WeaponFireMode.FullAuto:
                if (held && Time.time >= m_cooldownEndsAt)
                {
                    FireShot(1f);
                    m_cooldownEndsAt = Time.time + (m_settings != null ? m_settings.Cooldown : 0f);
                }
                break;

            case WeaponFireMode.Burst:
                if (held && m_burstRoutine == null && Time.time >= m_cooldownEndsAt)
                    m_burstRoutine = StartCoroutine(FireBurstRoutine());
                break;

            case WeaponFireMode.Charge:
                TickCharge(held);
                break;
        }
    }

    private IEnumerator FireBurstRoutine()
    {
        int shots = m_settings != null ? Mathf.Max(1, m_settings.BurstCount) : 1;
        float interval = m_settings != null ? m_settings.BurstInterval : 0f;

        for (int i = 0; i < shots; i++)
        {
            FireShot(1f);
            if (i < shots - 1)
                yield return new WaitForSeconds(interval);
        }

        m_cooldownEndsAt = Time.time + (m_settings != null ? m_settings.Cooldown : 0f);
        m_burstRoutine = null;
    }

    private void TickCharge(bool held)
    {
        if (m_settings == null) return;

        if (held)
        {
            if (Time.time < m_cooldownEndsAt) return;

            if (m_chargeStartTime < 0f)
                m_chargeStartTime = Time.time;

            float charged = Time.time - m_chargeStartTime;
            if (charged >= m_settings.MaxChargeSeconds)
            {
                FireShot(1f);
                m_chargeStartTime = -1f;
                m_cooldownEndsAt = Time.time + m_settings.Cooldown;
            }
            return;
        }

        if (m_chargeStartTime < 0f) return;

        float held_for = Time.time - m_chargeStartTime;
        if (held_for >= m_settings.MinChargeSeconds)
        {
            float t = Mathf.InverseLerp(m_settings.MinChargeSeconds, m_settings.MaxChargeSeconds, held_for);
            float mult = Mathf.Lerp(m_settings.MinChargeDamageMultiplier, 1f, Mathf.Clamp01(t));
            FireShot(mult);
            m_cooldownEndsAt = Time.time + m_settings.Cooldown;
        }
        m_chargeStartTime = -1f;
    }

    private void FireShot(float damageMultiplier)
    {
        if (m_pool == null || IsShootPointObstructed()) return;

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

        // Fall back to the weapon's intrinsic ammo when no player ammo took effect.
        // Intrinsic ammo is part of the weapon itself, so it is never consumed.
        if (ammoSettings == null && m_settings != null && m_settings.IntrinsicAmmo != null)
            ammoSettings = m_settings.IntrinsicAmmo;

        int pellets = m_settings != null ? Mathf.Max(1, m_settings.PelletsPerShot) : 1;
        for (int i = 0; i < pellets; i++)
            SpawnProjectile(ammoSettings, damageMultiplier);

        FlashMuzzle(ammoSettings);
        StartKickback();

        if (m_cameraShake != null && m_shootShakeAmplitude > 0f)
            m_cameraShake.Shake(m_shootShakeAmplitude, -m_shootPoint.right);

        OnFired?.Invoke(ammoSettings);
    }

    private void StartKickback()
    {
        if (m_settings == null) return;
        if (m_settings.KickbackDistance <= 0f || m_settings.KickbackDuration <= 0f) return;

        m_kickbackPeak = -m_settings.KickbackDistance;
        m_kickbackDuration = m_settings.KickbackDuration;
        m_kickbackTimeRemaining = m_kickbackDuration;
        CurrentKickbackOffset = m_kickbackPeak;
    }

    private void TickKickback()
    {
        if (m_kickbackTimeRemaining <= 0f)
        {
            if (CurrentKickbackOffset != 0f)
                CurrentKickbackOffset = 0f;
            return;
        }

        m_kickbackTimeRemaining -= Time.deltaTime;
        if (m_kickbackTimeRemaining <= 0f)
        {
            m_kickbackTimeRemaining = 0f;
            CurrentKickbackOffset = 0f;
            return;
        }

        float t = m_kickbackTimeRemaining / m_kickbackDuration;
        CurrentKickbackOffset = m_kickbackPeak * t;
    }

    private void SpawnProjectile(AmmoSettings ammoSettings, float damageMultiplier)
    {
        GameObject projectile = m_pool.GetPooledObject(m_projectile.gameObject);

        Quaternion rotation = m_shootPoint.rotation;
        float spread = m_settings != null ? m_settings.SpreadDegrees : 0f;
        if (spread > 0f)
            rotation *= Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-spread, spread));

        projectile.transform.SetPositionAndRotation(m_shootPoint.position, rotation);

        Projectile proj = projectile.GetComponent<Projectile>();
        proj.SetProjectilePrefab(m_projectile.gameObject);

        int flatBonus = 0;
        float mult = damageMultiplier;
        int pierce = 0;
        if (m_mutationManager != null)
        {
            flatBonus = m_mutationManager.GetFlatDamageBonus();
            mult *= m_mutationManager.GetDamageMultiplier();
            pierce = m_mutationManager.GetBonusPierce();
        }
        proj.SetMutationModifiers(flatBonus, mult, pierce);

        if (ammoSettings != null)
            proj.SetAmmoModifiers(ammoSettings);

        proj.Initialize();
    }

    private bool IsShootPointObstructed()
    {
        if (m_weaponHolder == null) return false;

        Vector2 origin = m_weaponHolder.position;
        Vector2 target = m_shootPoint.position;
        Vector2 direction = target - origin;

        return Physics2D.Raycast(origin, direction, direction.magnitude, GameConstants.Layers.WallsLayerMask);
    }

    private void FlashMuzzle(AmmoSettings ammoSettings)
    {
        if (m_muzzleFlashObject == null || m_muzzleFlashDuration <= 0f) return;

        if (m_muzzleFlashRoutine != null)
            StopCoroutine(m_muzzleFlashRoutine);

        if (m_muzzleFlashRenderer != null)
            m_muzzleFlashRenderer.color = ammoSettings != null ? ammoSettings.ProjectileColor : m_muzzleFlashDefaultTint;

        m_muzzleFlashObject.SetActive(true);
        m_muzzleFlashRoutine = StartCoroutine(HideMuzzleFlashAfterDelay());
    }

    private IEnumerator HideMuzzleFlashAfterDelay()
    {
        yield return new WaitForSeconds(m_muzzleFlashDuration);
        if (m_muzzleFlashObject != null)
            m_muzzleFlashObject.SetActive(false);
        m_muzzleFlashRoutine = null;
    }
}
