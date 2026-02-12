using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityServiceLocator;

namespace Gameplay.ObjectPool
{
    public class ObjectPool : MonoBehaviour
    {
        private Dictionary<int, IObjectPool<GameObject>> m_pools = new Dictionary<int, IObjectPool<GameObject>>();
        private Dictionary<int, IObjectPool<GameObject>> m_activeObjects = new Dictionary<int, IObjectPool<GameObject>>();

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
                    defaultCapacity: 10,
                    maxSize: 50
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
            // Give it back to the pool.
            ReturnGameObject(gameObject);
        }

        public void ReturnGameObject(GameObject gameObject)
        {
            if (m_activeObjects.TryGetValue(gameObject.GetInstanceID(), out var pool))
            {
                pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
