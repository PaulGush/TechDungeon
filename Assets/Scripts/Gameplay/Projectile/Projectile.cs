using System;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class Projectile : MonoBehaviour
{
    public event Action OnEntityImpact;
    public event Action OnWallImpact;

    [Header("References")]
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private ProjectileSettings m_settings;

    [Tooltip("Optional TrailRenderer on the projectile. Cleared on spawn to prevent pool-reuse streaks, and its start/end colors are driven by ammo color > trail override > sprite color.")]
    [SerializeField] private TrailRenderer m_trail;

    [Tooltip("Optional explicit color for the trail when no ammo is loaded. Leave alpha at zero to fall back to the sprite's color (for bullets whose trail should match their sprite). Set alpha > 0 to force a specific trail color distinct from the sprite tint (e.g. a white missile with an orange trail).")]
    [SerializeField] private Color m_trailColor = new Color(0f, 0f, 0f, 0f);

    [Header("Collision Filtering")]
    [SerializeField] private LayerMask m_damageLayers;
    [SerializeField] private LayerMask m_destroyLayers;

    [Header("Feedback")]
    [Tooltip("Impulse amplitude applied to the camera shake service each time this projectile damages an entity. Zero disables.")]
    [SerializeField] private float m_hitShakeAmplitude = 0.1f;

    [Tooltip("Pooled hit spark prefab spawned at the impact point each time this projectile damages an entity. Leave empty to skip.")]
    [SerializeField] private GameObject m_hitSparkPrefab;

    private ObjectPool m_pool;
    private int m_hitsBeforeDeath;
    private float m_returnTime;
    private bool m_destroyed;
    private CameraShake m_cameraShake;
    private Color m_defaultColor = Color.white;

    // Item modifiers
    private int m_bonusDamage;
    private float m_damageMultiplier = 1f;
    private int m_bonusPierce;

    // Ammo
    private AmmoSettings m_ammoSettings;
    private IAmmoEffect m_ammoEffect;
    private GameObject m_prefab;

    // Runtime crit tint override (alpha > 0 means active). Takes precedence over ammo color and
    // the authored trail override so the player sees an unambiguous "that was a crit" cue.
    private Color m_critTint;

    public void SetItemModifiers(int bonusDamage, float damageMultiplier, int bonusPierce)
    {
        m_bonusDamage = bonusDamage;
        m_damageMultiplier = damageMultiplier;
        m_bonusPierce = bonusPierce;
    }

    public void SetAmmoModifiers(AmmoSettings settings)
    {
        m_ammoSettings = settings;
        m_ammoEffect = settings != null ? settings.CreateEffect() : null;
        ApplyAmmoTint(settings);
    }

    public void SetAmmoEffect(AmmoSettings settings, IAmmoEffect effect)
    {
        m_ammoSettings = settings;
        m_ammoEffect = effect;
        ApplyAmmoTint(settings);
    }

    private void ApplyAmmoTint(AmmoSettings settings)
    {
        if (settings == null) return;

        // Crit tint locks the sprite color for this shot — skip the ammo overwrite so it wins.
        if (m_critTint.a > 0f) return;

        if (m_spriteRenderer != null)
            m_spriteRenderer.color = settings.ProjectileColor;
    }

    public void SetCritTint(Color tint)
    {
        m_critTint = tint;
        if (tint.a > 0f && m_spriteRenderer != null)
            m_spriteRenderer.color = tint;
    }

    public void SetProjectilePrefab(GameObject prefab) => m_prefab = prefab;

    private void Awake()
    {
        // Cache in Awake (not Start) so the authored prefab color is captured before
        // SetAmmoModifiers can run on the first shot — otherwise a first fire with an
        // ammo tint would bake the ammo color in as the "default".
        if (m_spriteRenderer != null)
            m_defaultColor = m_spriteRenderer.color;

        // Pool instances are Instantiate'd by ObjectPool, which has already registered
        // itself in its own Awake. CameraShake registers similarly; if it hasn't yet the
        // projectile simply skips shake feedback until it's available.
        ServiceLocator.Global.TryGet(out m_pool);
        ServiceLocator.Global.TryGet(out m_cameraShake);
    }

    public virtual void Initialize()
    {
        m_hitsBeforeDeath = m_settings.HitsBeforeDeath + m_bonusPierce + (m_ammoSettings != null ? m_ammoSettings.BonusPierce : 0);
        m_destroyed = false;
        // Sync the Rigidbody2D to the transform before setting velocity — when teleported
        // in the pool (transform.SetPositionAndRotation while inactive), the rigidbody's
        // internal position still holds the previous despawn location. WakeUp() ensures it
        // isn't still sleeping from the last deactivation.
        m_rigidbody2D.position = transform.position;
        m_rigidbody2D.WakeUp();
        // Interpolate so the transform advances between FixedUpdates instead of snapping
        // once per physics tick. At high render framerates a render frame often has zero
        // intervening FixedUpdates (fixedDeltaTime is 20ms; at 300fps a frame is ~3ms), so
        // with Interpolation.None the transform stayed at the spawn point for multiple
        // render frames and the TrailRenderer sampled the same position on consecutive
        // LateUpdates — no trail vertices, no visible trail.
        m_rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Set velocity directly rather than AddForce — AddForce in ForceMode2D.Force integrates
        // over one FixedUpdate, so if the first FixedUpdate is delayed the projectile has no
        // velocity when the trail first samples. Magnitude preserves the prior effective speed.
        m_rigidbody2D.linearVelocity = (Vector2)transform.right * (m_settings.Speed * Time.fixedDeltaTime);

        // Priority for trail tint: crit tint (if active) > explicit TrailColor override (if
        // alpha > 0) > ammo tint > sprite color. Crit wins over every other source so a crit
        // shot reads as a crit even when the projectile has a trail color override or ammo.
        if (m_trail != null)
        {
            Color tint;
            if (m_critTint.a > 0f)
                tint = m_critTint;
            else if (m_trailColor.a > 0f)
                tint = m_trailColor;
            else if (m_ammoSettings != null)
                tint = m_ammoSettings.ProjectileColor;
            else if (m_spriteRenderer != null)
                tint = m_spriteRenderer.color;
            else
                tint = Color.white;

            m_trail.startColor = tint;
            tint.a = 0f;
            m_trail.endColor = tint;
            m_trail.Clear();
            m_trail.emitting = true;
            // Seed a two-vertex tail proportional to the trail's own lifetime so the trail
            // starts at roughly half its steady-state length. A single-vertex trail renders
            // as nothing (the source of the original "delayed trail" bug), and a fixed-size
            // seed looks right only for short-lifetime trails — a 0.04s stub on a 0.3s trail
            // is only 13% of steady-state length, which reads as a visible ramp-up. A full-
            // lifetime seed looks like a static streak extending far behind the shooter.
            // Half of trail.time is the middle ground: matches long trails without extending
            // past the shooter, and stays proportional for short trails.
            Vector3 spawn = transform.position;
            Vector3 tailBehind = spawn - (Vector3)m_rigidbody2D.linearVelocity * (m_trail.time * 0.5f);
            m_trail.AddPosition(tailBehind);
            m_trail.AddPosition(spawn);
        }

        m_returnTime = Time.time + m_settings.Lifetime;
    }

    private void Update()
    {
        // Lifetime check inline instead of StartCoroutine(ReturnAfter) — the coroutine
        // allocated a WaitForSeconds + iterator per spawn, which adds up at high fire
        // rates. An Update check is essentially free and cancels naturally when the
        // GameObject is disabled.
        if (Time.time >= m_returnTime && m_pool != null)
            m_pool.ReturnGameObject(gameObject);
    }

    private void OnDisable()
    {
        m_rigidbody2D.linearVelocity = Vector2.zero;

        // Reset modifiers for pool reuse
        m_bonusDamage = 0;
        m_damageMultiplier = 1f;
        m_bonusPierce = 0;
        m_ammoSettings = null;
        m_ammoEffect = null;
        m_critTint = default;

        if (m_spriteRenderer != null)
            m_spriteRenderer.color = m_defaultColor;

        if (m_trail != null)
        {
            m_trail.emitting = false;
            m_trail.Clear();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Guard against extra trigger callbacks that Unity already queued for this physics
        // step after we decided to destroy the projectile — otherwise clustered enemies can
        // each take damage from what should have been a single non-piercing hit.
        if (m_destroyed) return;

        int layerFlag = 1 << other.gameObject.layer;
        bool hitDestroyLayer = (layerFlag & m_destroyLayers) != 0;
        bool hitDamageLayer = (layerFlag & m_damageLayers) != 0;
        if (!hitDestroyLayer && !hitDamageLayer) return;

        AmmoEffectContext ctx = BuildContext();

        if (hitDestroyLayer)
        {
            if (m_ammoEffect != null && m_ammoEffect.TryPreventDestroy(ctx))
                return;

            m_destroyed = true;
            m_ammoEffect?.OnDestroy(ctx);
            SpawnHitSpark(alignToWallNormal: true);
            OnWallImpact?.Invoke();
            m_pool.ReturnGameObject(gameObject);
            return;
        }

        // Only count this as a pierce-consuming hit if we actually found an EntityHealth
        // to damage. Passing through damage-layer colliders that don't represent a damageable
        // entity (boss attack hitboxes like the flamethrower trigger, or any future hurtbox
        // that forwards hits elsewhere) would otherwise silently consume pierces and
        // desync the destroy check from the damage check.
        if (!other.gameObject.TryGetComponent(out EntityHealth entityHealth))
            return;

        // Decide destruction up-front and flag it before any side-effecting calls so a
        // same-physics-step re-entry from a sibling collider can't slip past the m_destroyed
        // guard above and land a second hit.
        bool willDestroy = m_hitsBeforeDeath-- <= 0;
        if (willDestroy)
            m_destroyed = true;

        int totalDamage = Mathf.RoundToInt((m_settings.Damage + m_bonusDamage) * m_damageMultiplier);
        entityHealth.TakeDamage(totalDamage);

        if (m_cameraShake != null && m_hitShakeAmplitude > 0f)
            m_cameraShake.Shake(m_hitShakeAmplitude);

        m_ammoEffect?.OnHit(ctx);

        OnEntityImpact?.Invoke();

        if (willDestroy)
        {
            m_ammoEffect?.OnDestroy(ctx);
            SpawnHitSpark(alignToWallNormal: false);
            m_pool.ReturnGameObject(gameObject);
        }
    }

    private void SpawnHitSpark(bool alignToWallNormal)
    {
        if (m_hitSparkPrefab == null || m_pool == null) return;

        GameObject spark = m_pool.GetPooledObject(m_hitSparkPrefab);

        Vector2 velocity = m_rigidbody2D.linearVelocity;
        Vector2 position = transform.position;
        Quaternion rotation;

        if (alignToWallNormal && velocity.sqrMagnitude > 0.0001f)
        {
            // Raycast from behind the projectile along its flight to find the exact wall
            // surface it hit. The raycast normal gives a true perpendicular for the spark.
            Vector2 velocityDir = velocity.normalized;
            Vector2 castStart = position - velocityDir * 0.5f;
            RaycastHit2D hit = Physics2D.Raycast(castStart, velocityDir, 1f, m_destroyLayers);
            if (hit.collider != null)
            {
                position = hit.point;
                rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg + 180f);
            }
            else
            {
                // Fallback: flip the spark to face opposite to the velocity so it still reads
                // as an outward splash rather than continuing through the wall.
                rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-velocityDir.y, -velocityDir.x) * Mathf.Rad2Deg + 180f);
            }
        }
        else
        {
            rotation = velocity.sqrMagnitude > 0.0001f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg)
                : transform.rotation;
        }

        spark.transform.SetPositionAndRotation(position, rotation);

        if (m_ammoSettings != null && spark.TryGetComponent(out PooledEffect effect))
            effect.SetTint(m_ammoSettings.ProjectileColor);
    }

    private AmmoEffectContext BuildContext()
    {
        return new AmmoEffectContext
        {
            Position = transform.position,
            Velocity = m_rigidbody2D.linearVelocity,
            BonusDamage = m_bonusDamage,
            DamageMultiplier = m_damageMultiplier,
            DamageLayers = m_damageLayers,
            DestroyLayers = m_destroyLayers,
            Pool = m_pool,
            ProjectilePrefab = m_prefab,
            Rigidbody = m_rigidbody2D,
            Transform = transform
        };
    }
}
