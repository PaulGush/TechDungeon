using UnityEngine;
using UnityEngine.Pool;

namespace ObjectPool
{
    public class SimplePool : MonoBehaviour
    {
        public static SimplePool Instance { get; private set; }
        
        private IObjectPool<GameObject> _pool;
        
        private GameObject Template;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            CreatePool();
        }

        private void CreatePool(int defaultCapacity = 10, int maxSize = 50)
        {
            // Create a pool with the four core callbacks.
            _pool = new ObjectPool<GameObject>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyItem,
                collectionCheck: true,   // helps catch double-release mistakes
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public GameObject GetPooledObject(GameObject template)
        {
            Template = template;
            return _pool.Get();
        }

        // Creates a new pooled GameObject the first time (and whenever the pool needs more).
        private GameObject CreateItem()
        {
            GameObject newGameObject = Instantiate(Template, transform, true);
            newGameObject.SetActive(false);
            return newGameObject;
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
            Destroy(gameObject);
        }
        
        public System.Collections.IEnumerator ReturnAfter(GameObject gameObject, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            // Give it back to the pool.
            _pool.Release(gameObject);
        }

        public void ReturnGameobject(GameObject gameObject)
        {
            _pool.Release(gameObject);
        }
    }
}
