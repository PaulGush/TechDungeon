using UnityEngine;

namespace UnityServiceLocator {
    [AddComponentMenu("ServiceLocator/ServiceLocator Global")]
    public class ServiceLocatorGlobal : Bootstrapper {
        [SerializeField] bool m_dontDestroyOnLoad = true;
        
        protected override void Bootstrap() {
            Container.ConfigureAsGlobal(m_dontDestroyOnLoad);
        }
    }
}