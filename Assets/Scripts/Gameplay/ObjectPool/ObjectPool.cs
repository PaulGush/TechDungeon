using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityServiceLocator;

namespace Gameplay.ObjectPool
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Initial pool capacity allocated the first time a prefab is requested.")]
        private int m_defaultCapacity = 10;

        [SerializeField, Min(1), Tooltip("Upper bound for retained instances; instances beyond this are destroyed on release.")]
        private int m_maxPoolSize = 50;

        private readonly Dictionary<int, IObjectPool<GameObject>> m_pools = new Dictionary<int, IObjectPool<GameObject>>();
        private readonly Dictionary<int, IObjectPool<GameObject>> m_activeObjects = new Dictionary<int, IObjectPool<GameObject>>();

        void Awake()
        {
            ServiceLocator.Global.Register(this);
        }

        public GameObject GetPooledObject(GameObject template)
        {
            int id = template.GetInstanceID();
            if (!m_pools.TryGetValue(id, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        GameObject newGameObject = Instantiate(template, transform, true);
                        newGameObject.SetActive(false);
                        return newGameObject;
                    },
                    actionOnGet: OnGet,
                    actionOnRelease: OnRelease,
                    actionOnDestroy: OnDestroyItem,
                    collectionCheck: true,
                    defaultCapacity: m_defaultCapacity,
                    maxSize: m_maxPoolSize
                );
                m_pools[id] = pool;
            }

            GameObject instance = pool.Get();
            m_activeObjects[instance.GetInstanceID()] = pool;
            return instance;
        }

        // Called when an item is taken from the pool.
        private void OnGet(GameObject gameObject)
        {
            if (gameObject == null) return;
            gameObject.SetActive(true);
        }

        // Called when an item is returned to the pool.
        private void OnRelease(GameObject gameObject)
        {
            gameObject.transform.position = Vector3.zero;
            gameObject.SetActive(false);
        }

        // Called when the pool decides to destroy an item (e.g., above max size).
        private void OnDestroyItem(GameObject gameObject)
        {
            m_activeObjects.Remove(gameObject.GetInstanceID());
            Destroy(gameObject);
        }
    
        public System.Collections.IEnumerator ReturnAfter(GameObject gameObject, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (gameObject == null) yield break;
            ReturnGameObject(gameObject);
        }

        public bool ReturnGameObject(GameObject gameObject)
        {
            if (gameObject == null) return false;

            int id = gameObject.GetInstanceID();
            if (m_activeObjects.TryGetValue(id, out var pool))
            {
                m_activeObjects.Remove(id);
                pool.Release(gameObject);
                return true;
            }

            return false;
        }

        public void ClearAll()
        {
            m_activeObjects.Clear();

            foreach (var pool in m_pools.Values)
            {
                pool.Clear();
            }

            m_pools.Clear();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
