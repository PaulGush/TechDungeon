using System;
using System.Collections;
using Gameplay.ObjectPool;
using Input;
using PlayerObject;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityServiceLocator;

public class WeaponShooting : MonoBehaviour, IWeapon
{
    public event Action<AmmoSettings> OnFired;
    public event Action<int, int> OnMagazineChanged;
    public event Action<float> OnReloadStarted;
    public event Action OnReloadCompleted;
    public event Action OnReloadCancelled;

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

    [Tooltip("Optional Light2D on (or under) the muzzle flash object. Its color is tinted by the ammo color on fire and restored to its authored default otherwise. Auto-resolved from the muzzle flash object if left empty.")]
    [SerializeField] private Light2D m_muzzleFlashLight;

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
    private Color m_muzzleFlashDefaultLightColor = Color.white;
    private Lootable m_lootable;

    private bool m_equipped;
    private float m_cooldownEndsAt;
    private Coroutine m_burstRoutine;
    private float m_chargeStartTime = -1f;
    private float m_sustainedFireStart = -1f;

    private float m_kickbackTimeRemaining;
    private float m_kickbackDuration;
    private float m_kickbackPeak;

    private AmmoType m_loadedAmmoType = AmmoType.Standard;
    private int m_magazineCurrent;
    private bool m_isReloading;
    private Coroutine m_reloadRoutine;

    public Vector2 ShootPointPosition => m_shootPoint.position;
    public int MagazineCurrent => m_magazineCurrent;
    public int MagazineMax => m_settings != null ? m_settings.MagazineSize : 0;
    public bool IsReloading => m_isReloading;
    public AmmoType LoadedAmmoType => m_loadedAmmoType;
    public bool UsesMagazine => m_settings != null && m_settings.MagazineSize > 0;

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

        // Cache the muzzle flash's authored default tints once at Start so they
        // survive ammo-color overrides. Doing this in Equip was fragile because
        // a re-equip after a tinted shot would have captured the ammo color as
        // the "default".
        if (m_muzzleFlashObject != null)
        {
            m_muzzleFlashRenderer = m_muzzleFlashObject.GetComponentInChildren<SpriteRenderer>(true);
            if (m_muzzleFlashRenderer != null)
                m_muzzleFlashDefaultTint = m_muzzleFlashRenderer.color;

            if (m_muzzleFlashLight == null)
                m_muzzleFlashLight = m_muzzleFlashObject.GetComponentInChildren<Light2D>(true);
            if (m_muzzleFlashLight != null)
                m_muzzleFlashDefaultLightColor = m_muzzleFlashLight.color;
        }
    }

    public void Equip()
    {
        m_weaponHolder = GetComponentInParent<WeaponHolder>().transform;
        m_equipped = true;
        m_cooldownEndsAt = 0f;
        m_chargeStartTime = -1f;

        if (m_lootable == null)
            m_lootable = GetComponent<Lootable>() ?? GetComponentInParent<Lootable>() ?? GetComponentInChildren<Lootable>();

        if (m_settings == null)
            Debug.LogWarning($"WeaponShooting on '{name}' has no WeaponSettings assigned — falling back to unthrottled semi-auto.", this);

        if (m_muzzleFlashObject != null)
            m_muzzleFlashObject.SetActive(false);

        m_magazineCurrent = 0;
        m_isReloading = false;
        m_loadedAmmoType = m_ammoManager != null && m_ammoManager.CurrentAmmoSettings != null
            ? m_ammoManager.CurrentAmmoSettings.Type
            : AmmoType.Standard;

        if (UsesMagazine)
        {
            LoadMagazineFromPool();
        }
        OnMagazineChanged?.Invoke(m_magazineCurrent, MagazineMax);

        if (m_inputReader != null)
        {
            m_inputReader.Reload += OnReloadInput;
            m_inputReader.Roll += OnRollInput;
        }
        if (m_ammoManager != null)
            m_ammoManager.OnAmmoChanged += OnAmmoTypeSwitched;
    }

    public void Unequip()
    {
        m_equipped = false;
        m_chargeStartTime = -1f;
        m_sustainedFireStart = -1f;
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

        CancelReload();

        if (UsesMagazine && m_magazineCurrent > 0 && m_ammoManager != null)
            m_ammoManager.ReturnToPool(m_loadedAmmoType, m_magazineCurrent);
        m_magazineCurrent = 0;

        if (m_inputReader != null)
        {
            m_inputReader.Reload -= OnReloadInput;
            m_inputReader.Roll -= OnRollInput;
        }
        if (m_ammoManager != null)
            m_ammoManager.OnAmmoChanged -= OnAmmoTypeSwitched;
    }

    private void Update()
    {
        TickKickback();

        if (!m_equipped || m_inputReader == null) return;

        bool held = m_inputReader.IsAttackHeld;
        if (held && m_sustainedFireStart < 0f)
            m_sustainedFireStart = Time.time;
        else if (!held)
            m_sustainedFireStart = -1f;

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

        if (UsesMagazine)
        {
            if (m_isReloading) return;
            if (m_magazineCurrent <= 0)
            {
                StartReload();
                return;
            }
        }

        bool efficient = m_ammoManager != null && m_ammoManager.RollAmmoEfficiency();
        AmmoSettings ammoSettings = ResolveAmmoForShot(efficient);

        // Fall back to the weapon's intrinsic ammo when no player ammo is active.
        // Intrinsic ammo is part of the weapon itself and doesn't use the type-pool model.
        if (ammoSettings == null && m_settings != null && m_settings.IntrinsicAmmo != null)
            ammoSettings = m_settings.IntrinsicAmmo;

        if (UsesMagazine && !efficient)
        {
            m_magazineCurrent--;
            OnMagazineChanged?.Invoke(m_magazineCurrent, MagazineMax);
        }

        int pellets = m_settings != null ? Mathf.Max(1, m_settings.PelletsPerShot) : 1;
        for (int i = 0; i < pellets; i++)
            SpawnProjectile(ammoSettings, damageMultiplier);

        FlashMuzzle(ammoSettings);
        StartKickback();

        if (m_cameraShake != null && m_shootShakeAmplitude > 0f)
            m_cameraShake.Shake(m_shootShakeAmplitude, -m_shootPoint.right);

        OnFired?.Invoke(ammoSettings);

        if (UsesMagazine && m_magazineCurrent <= 0)
            StartReload();
    }

    private AmmoSettings ResolveAmmoForShot(bool efficient)
    {
        if (m_ammoManager == null) return null;

        // Magazine path: the mag tracks which type was loaded, so shots use that until reload.
        if (UsesMagazine)
        {
            return m_loadedAmmoType != AmmoType.Standard
                ? m_ammoManager.GetSettingsForType(m_loadedAmmoType)
                : null;
        }

        // Non-magazine path: draw one round from the live ammo type's pool per shot, matching
        // the pre-reload behavior. Efficiency mutation lets a shot pass without drawing.
        AmmoSettings current = m_ammoManager.CurrentAmmoSettings;
        if (current == null || current.Type == AmmoType.Standard) return null;

        if (efficient) return current;

        if (m_ammoManager.TryDrawFromPool(current.Type, 1) > 0)
        {
            if (m_ammoManager.GetAmmoCount(current.Type) <= 0)
                m_ammoManager.CycleToStandard();
            return current;
        }
        return null;
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
        Quaternion rotation = m_shootPoint.rotation;
        float spread = GetCurrentSpread();
        if (spread > 0f)
            rotation *= Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-spread, spread));

        int flatBonus = 0;
        float mult = damageMultiplier;
        int pierce = 0;
        if (m_mutationManager != null)
        {
            flatBonus = m_mutationManager.GetFlatDamageBonus();
            mult *= m_mutationManager.GetDamageMultiplier();
            pierce = m_mutationManager.GetBonusPierce();
        }
        if (m_lootable != null)
            mult *= LootableRarity.GetDamageMultiplier(m_lootable.Rarity);

        ProjectileSpawner.Spawn(
            m_pool, m_projectile.gameObject, m_shootPoint.position, rotation,
            bonusDamage: flatBonus, damageMultiplier: mult, bonusPierce: pierce,
            ammoSettings: ammoSettings);
    }

    // Ramps SpreadDegrees up toward MaxSpreadDegrees over SpreadRampDuration seconds of
    // continuously held attack input. The timer resets on release and on reload start so
    // short controlled bursts stay accurate while sustained full-auto fire loses precision.
    // Weapons with a zero ramp duration or no configured max keep the flat SpreadDegrees.
    private float GetCurrentSpread()
    {
        if (m_settings == null) return 0f;

        float baseSpread = m_settings.SpreadDegrees;
        float maxSpread = m_settings.MaxSpreadDegrees;
        float rampDuration = m_settings.SpreadRampDuration;

        if (rampDuration <= 0f || maxSpread <= baseSpread || m_sustainedFireStart < 0f)
            return baseSpread;

        float heldFor = Time.time - m_sustainedFireStart;
        float t = Mathf.Clamp01(heldFor / rampDuration);
        return Mathf.Lerp(baseSpread, maxSpread, t);
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

        if (m_muzzleFlashLight != null)
            m_muzzleFlashLight.color = ammoSettings != null ? ammoSettings.ProjectileColor : m_muzzleFlashDefaultLightColor;

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

    private void OnReloadInput()
    {
        if (!m_equipped || !UsesMagazine) return;
        if (m_isReloading) return;
        if (m_magazineCurrent >= MagazineMax) return;
        StartReload();
    }

    private void OnRollInput()
    {
        if (m_isReloading)
            CancelReload();
    }

    private void OnAmmoTypeSwitched(AmmoSettings newSettings)
    {
        if (!m_equipped) return;
        AmmoType newType = newSettings != null ? newSettings.Type : AmmoType.Standard;
        if (newType == m_loadedAmmoType) return;

        CancelReload();

        if (UsesMagazine && m_magazineCurrent > 0 && m_ammoManager != null)
            m_ammoManager.ReturnToPool(m_loadedAmmoType, m_magazineCurrent);

        m_loadedAmmoType = newType;
        m_magazineCurrent = 0;
        OnMagazineChanged?.Invoke(m_magazineCurrent, MagazineMax);

        if (UsesMagazine)
            StartReload();
    }

    private void StartReload()
    {
        if (!UsesMagazine || m_isReloading) return;

        // If the loaded type is depleted (pool empty, mag empty), fall back to standard
        // so the player isn't stuck trying to reload nothing. Cycling to standard will
        // re-enter this method via OnAmmoTypeSwitched.
        if (m_loadedAmmoType != AmmoType.Standard && m_ammoManager != null
            && m_ammoManager.GetAmmoCount(m_loadedAmmoType) <= 0 && m_magazineCurrent <= 0)
        {
            m_ammoManager.CycleToStandard();
            return;
        }

        // Reset the sustained-fire spread ramp — reloading always breaks a continuous
        // burst, so post-reload shots should start from base accuracy even if the player
        // keeps the attack button held through the reload.
        m_sustainedFireStart = -1f;

        float duration = m_settings != null ? m_settings.ReloadDuration : 0f;
        m_isReloading = true;
        OnReloadStarted?.Invoke(duration);

        if (duration <= 0f)
        {
            FinishReload();
            return;
        }

        m_reloadRoutine = StartCoroutine(ReloadCoroutine(duration));
    }

    private IEnumerator ReloadCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        FinishReload();
    }

    private void FinishReload()
    {
        m_reloadRoutine = null;
        m_isReloading = false;
        LoadMagazineFromPool();
        OnMagazineChanged?.Invoke(m_magazineCurrent, MagazineMax);
        OnReloadCompleted?.Invoke();
    }

    private void CancelReload()
    {
        if (m_reloadRoutine != null)
        {
            StopCoroutine(m_reloadRoutine);
            m_reloadRoutine = null;
        }
        if (m_isReloading)
        {
            m_isReloading = false;
            OnReloadCancelled?.Invoke();
        }
    }

    private void LoadMagazineFromPool()
    {
        if (!UsesMagazine) return;
        int needed = MagazineMax - m_magazineCurrent;
        if (needed <= 0) return;

        int drawn = m_ammoManager != null
            ? m_ammoManager.TryDrawFromPool(m_loadedAmmoType, needed)
            : needed; // No manager — treat as infinite so the weapon stays functional
        m_magazineCurrent += drawn;
    }
}
