using System;
using Gameplay.ObjectPool;
using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(Lootable))]
public class Pickup : MonoBehaviour
{
    private IPickupEffect m_effect;
    private ObjectPool m_pool;

    private Lootable m_lootable;
    // Latches on the first successful Apply so a second OnTriggerEnter2D fired in the same
    // frame (e.g. from a second collider on the player) can't apply the effect twice — that
    // was filling two ability slots from a single ability drop.
    private bool m_collected;

    public Action OnPickedUp;

    private void Awake()
    {
        m_effect = GetComponent<IPickupEffect>();
        m_lootable = GetComponent<Lootable>();
    }

    private void OnEnable()
    {
        ServiceLocator.Global.TryGet(out ObjectPool pool);
        m_pool = pool;
        m_collected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_collected) return;
        if (m_lootable.IsSpawning) return;
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        if (m_effect == null) return;

        if (m_lootable.OnCollected != null)
        {
            m_lootable.OnCollected.Invoke();
            m_lootable.OnCollected = null;
        }

        if (!m_effect.Apply(other.gameObject)) return;

        m_collected = true;
        OnPickedUp?.Invoke();

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }
}
