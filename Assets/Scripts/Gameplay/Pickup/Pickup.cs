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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (m_lootable.IsSpawning) return;
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;
        if (m_effect == null) return;

        if (m_lootable.OnCollected != null)
        {
            m_lootable.OnCollected.Invoke();
            m_lootable.OnCollected = null;
        }

        if (!m_effect.Apply(other.gameObject)) return;

        OnPickedUp?.Invoke();

        if (m_pool == null || !m_pool.ReturnGameObject(gameObject))
            Destroy(gameObject);
    }
}
