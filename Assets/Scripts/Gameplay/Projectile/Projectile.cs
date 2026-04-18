using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

public class Projectile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D m_rigidbody2D;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private ProjectileSettings m_settings;

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
    private Coroutine m_returnCoroutine;
    private bool m_destroyed;
    private CameraShake m_cameraShake;

    // Mutation modifiers
    private int m_bonusDamage;
    private float m_damageMultiplier = 1f;
    private int m_bonusPierce;

    // Ammo
    private AmmoSettings m_ammoSettings;
    private IAmmoEffect m_ammoEffect;
    private GameObject m_prefab;

    public void SetMutationModifiers(int bonusDamage, float damageMultiplier, int bonusPierce)
    {
        m_bonusDamage = bonusDamage;
        m_damageMultiplier = damageMultiplier;
        m_bonusPierce = bonusPierce;
    }

    public void SetAmmoModifiers(AmmoSettings settings)
    {
        m_ammoSettings = settings;
        m_ammoEffect = settings != null ? settings.CreateEffect() : null;

        if (m_spriteRenderer != null && settings != null)
            m_spriteRenderer.color = settings.ProjectileColor;
    }

    public void SetAmmoEffect(AmmoSettings settings, IAmmoEffect effect)
    {
        m_ammoSettings = settings;
        m_ammoEffect = effect;

        if (m_spriteRenderer != null && settings != null)
            m_spriteRenderer.color = settings.ProjectileColor;
    }

    public void SetProjectilePrefab(GameObject prefab) => m_prefab = prefab;

    public virtual void Initialize()
    {
        if (m_pool == null)
        {
            ServiceLocator.Global.TryGet(out ObjectPool pool);
            m_pool = pool;
        }

        if (m_cameraShake == null)
            ServiceLocator.Global.TryGet(out m_cameraShake);

        m_hitsBeforeDeath = m_settings.HitsBeforeDeath + m_bonusPierce + (m_ammoSettings != null ? m_ammoSettings.BonusPierce : 0);
        m_destroyed = false;
        m_rigidbody2D.AddForce( transform.right * m_settings.Speed);

        m_returnCoroutine = StartCoroutine(m_pool.ReturnAfter(gameObject, m_settings.Lifetime));
    }

    private void OnDisable()
    {
        if (m_returnCoroutine != null)
        {
            StopCoroutine(m_returnCoroutine);
            m_returnCoroutine = null;
        }
        m_rigidbody2D.linearVelocity = Vector2.zero;

        // Reset modifiers for pool reuse
        m_bonusDamage = 0;
        m_damageMultiplier = 1f;
        m_bonusPierce = 0;
        m_ammoSettings = null;
        m_ammoEffect = null;

        if (m_spriteRenderer != null)
            m_spriteRenderer.color = Color.white;
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
            SpawnHitSpark();
            m_pool.ReturnGameObject(gameObject);
            return;
        }

        if (other.gameObject.TryGetComponent(out EntityHealth entityHealth))
        {
            int totalDamage = Mathf.RoundToInt((m_settings.Damage + m_bonusDamage) * m_damageMultiplier);
            entityHealth.TakeDamage(totalDamage);

            if (m_cameraShake != null && m_hitShakeAmplitude > 0f)
                m_cameraShake.Shake(m_hitShakeAmplitude);
        }

        m_ammoEffect?.OnHit(ctx);

        if (m_hitsBeforeDeath-- <= 0)
        {
            m_destroyed = true;
            m_ammoEffect?.OnDestroy(ctx);
            SpawnHitSpark();
            m_pool.ReturnGameObject(gameObject);
        }
    }

    private void SpawnHitSpark()
    {
        if (m_hitSparkPrefab == null || m_pool == null) return;

        GameObject spark = m_pool.GetPooledObject(m_hitSparkPrefab);

        Vector2 velocity = m_rigidbody2D.linearVelocity;
        Quaternion rotation = velocity.sqrMagnitude > 0.0001f
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg)
            : transform.rotation;
        spark.transform.SetPositionAndRotation(transform.position, rotation);

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
