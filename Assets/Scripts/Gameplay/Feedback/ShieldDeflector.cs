using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// While the Phase Shield is up, snuffs incoming hostile projectiles at the bubble's edge —
/// returns them to the pool and pops a spark on the surface — so shots visibly stop at the
/// shield instead of sailing through to the (already invulnerable) player.
/// <para>
/// Sits on a trigger collider that belongs to the player's compound rigidbody, so it tracks the
/// player and exerts no physical push. The collider is disabled except while
/// <see cref="PlayerStatusEffects.BuffKind.Invulnerable"/> is active. A projectile counts as
/// hostile when <see cref="Projectile.TargetsPlayer"/> is true; the projectile's own trigger
/// handler harmlessly ignores this collider because the GameObject carries no <c>EntityHealth</c>.
/// Keep the collider radius in sync with <see cref="ShieldVfx"/>'s ring radius.
/// </para>
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class ShieldDeflector : MonoBehaviour
{
    [SerializeField] private CircleCollider2D m_collider;

    [Tooltip("Optional pooled spark popped on the bubble surface where a shot is snuffed (uses PooledEffect.SetTint). Leave empty to skip.")]
    [SerializeField] private GameObject m_impactSparkPrefab;

    [Tooltip("Tint applied to the impact spark — match the bubble colour.")]
    [SerializeField] private Color m_sparkTint = new Color(0.5f, 1.7f, 1.2f, 1f);

    private PlayerStatusEffects m_status;
    private ObjectPool m_pool;

    private void Reset() => m_collider = GetComponent<CircleCollider2D>();

    private void Awake()
    {
        if (m_collider == null) m_collider = GetComponent<CircleCollider2D>();
        m_collider.isTrigger = true;
        m_collider.enabled = false;
    }

    private void Start()
    {
        ServiceLocator.Global.TryGet(out m_pool);
        if (!ServiceLocator.Global.TryGet(out m_status)) return;

        m_status.OnBuffStarted += OnBuffStarted;
        m_status.OnBuffEnded += OnBuffEnded;
        m_collider.enabled = m_status.IsActive(PlayerStatusEffects.BuffKind.Invulnerable);
    }

    private void OnDestroy()
    {
        if (m_status == null) return;
        m_status.OnBuffStarted -= OnBuffStarted;
        m_status.OnBuffEnded -= OnBuffEnded;
    }

    private void OnBuffStarted(PlayerStatusEffects.BuffKind kind, float _)
    {
        if (kind == PlayerStatusEffects.BuffKind.Invulnerable) m_collider.enabled = true;
    }

    private void OnBuffEnded(PlayerStatusEffects.BuffKind kind)
    {
        if (kind == PlayerStatusEffects.BuffKind.Invulnerable) m_collider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Projectile projectile = other.GetComponentInParent<Projectile>();
        if (projectile == null || !projectile.TargetsPlayer) return;

        SpawnSpark(other.transform.position);

        // ReturnGameObject runs the projectile's OnDisable (resets it for pool reuse) and is a
        // no-op if it's already back in the pool. Fall back to deactivating an untracked instance.
        if (m_pool == null || !m_pool.ReturnGameObject(projectile.gameObject))
            projectile.gameObject.SetActive(false);
    }

    private void SpawnSpark(Vector2 shotPosition)
    {
        if (m_impactSparkPrefab == null || m_pool == null) return;

        Vector2 centre = transform.position;
        Vector2 toShot = shotPosition - centre;
        Vector2 dir = toShot.sqrMagnitude > 1e-6f ? toShot.normalized : Vector2.up;
        Vector2 surface = centre + dir * m_collider.radius;
        Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        GameObject spark = m_pool.GetPooledObject(m_impactSparkPrefab, surface, rot);
        if (spark != null && spark.TryGetComponent(out PooledEffect pe))
            pe.SetTint(m_sparkTint);
    }
}
