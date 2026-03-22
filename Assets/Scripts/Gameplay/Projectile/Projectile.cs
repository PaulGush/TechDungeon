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

    private ObjectPool m_pool;
    private int m_hitsBeforeDeath;
    private Coroutine m_returnCoroutine;

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

        m_hitsBeforeDeath = m_settings.HitsBeforeDeath + m_bonusPierce + (m_ammoSettings != null ? m_ammoSettings.BonusPierce : 0);
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
        int layerFlag = 1 << other.gameObject.layer;

        if ((layerFlag & m_destroyLayers) != 0)
        {
            if (m_ammoEffect != null && m_ammoEffect.TryPreventDestroy(BuildContext()))
                return;

            m_ammoEffect?.OnDestroy(BuildContext());
            m_pool.ReturnGameObject(gameObject);
            return;
        }

        if ((layerFlag & m_damageLayers) == 0) return;

        if (other.gameObject.TryGetComponent<EntityHealth>(out var entityHealth))
        {
            int totalDamage = Mathf.RoundToInt((m_settings.Damage + m_bonusDamage) * m_damageMultiplier);
            entityHealth.TakeDamage(totalDamage);
        }

        m_ammoEffect?.OnHit(BuildContext());

        if (m_hitsBeforeDeath-- <= 0)
        {
            m_ammoEffect?.OnDestroy(BuildContext());
            m_pool.ReturnGameObject(gameObject);
        }
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
