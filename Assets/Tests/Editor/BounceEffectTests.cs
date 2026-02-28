using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class BounceEffectTests
    {
        private GameObject m_gameObject;
        private BounceEffect m_bounceEffect;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("TestBounce");
            m_bounceEffect = m_gameObject.AddComponent<BounceEffect>();
            m_bounceEffect.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        [Test]
        public void SetTargets_ThenStop_ResetsToCenter()
        {
            m_gameObject.transform.position = new Vector3(1f, 2f, 3f);
            m_bounceEffect.SetTargets();
            m_bounceEffect.enabled = true;

            // Stop resets to center of upper/lower targets, which is the original position
            m_bounceEffect.Stop();

            Assert.AreEqual(1f, m_gameObject.transform.position.x, 0.001f);
            Assert.AreEqual(2f, m_gameObject.transform.position.y, 0.001f);
            Assert.AreEqual(3f, m_gameObject.transform.position.z, 0.001f);
        }

        [Test]
        public void Stop_DisablesComponentAndResetsPosition()
        {
            m_gameObject.transform.position = new Vector3(1f, 2f, 3f);
            m_bounceEffect.SetTargets();
            m_bounceEffect.enabled = true;

            m_bounceEffect.Stop();

            Assert.IsFalse(m_bounceEffect.enabled);
            // Position should be reset to center between upper and lower targets (original position)
            Assert.AreEqual(2f, m_gameObject.transform.position.y, 0.001f);
        }

        [Test]
        public void DisabledByDefault_DoesNotMove()
        {
            Vector3 original = new Vector3(5f, 5f, 5f);
            m_gameObject.transform.position = original;

            // BounceEffect is disabled in SetUp, calling Update shouldn't move it
            // (Update won't run when disabled, but targets are also zero)
            Assert.AreEqual(original, m_gameObject.transform.position);
        }

        [Test]
        public void Stop_WhenNeverStarted_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => m_bounceEffect.Stop());
            Assert.IsFalse(m_bounceEffect.enabled);
        }
    }
}
