using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class LootableTests
    {
        private GameObject m_gameObject;
        private Lootable m_lootable;

        [SetUp]
        public void SetUp()
        {
            m_gameObject = new GameObject("TestLootable");
            m_lootable = m_gameObject.AddComponent<Lootable>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_gameObject);
        }

        [Test]
        public void IsSpawning_DefaultsFalse()
        {
            Assert.IsFalse(m_lootable.IsSpawning);
        }

        [Test]
        public void StartSpawnSequence_SetsIsSpawningTrue()
        {
            m_lootable.SetTargetPosition(Vector3.one);
            m_lootable.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_lootable.StartSpawnSequence(0.5f, 0.01f, 0f);

            Assert.IsTrue(m_lootable.IsSpawning);
        }

        [Test]
        public void StartSpawnSequence_ScaleStartsSmall()
        {
            m_gameObject.transform.localScale = Vector3.one;
            m_lootable.SetTargetPosition(Vector3.one);
            m_lootable.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_lootable.StartSpawnSequence(0.5f, 0.01f, 0f);

            // Coroutine runs one step before yielding, so scale is small but not zero
            Assert.Less(m_gameObject.transform.localScale.x, 0.1f);
        }

        [Test]
        public void CancelSpawn_WhenSpawning_SetsIsSpawningFalse()
        {
            m_lootable.SetTargetPosition(Vector3.one);
            m_lootable.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_lootable.StartSpawnSequence(0.5f, 0.01f, 0f);

            m_lootable.CancelSpawn();

            Assert.IsFalse(m_lootable.IsSpawning);
        }

        [Test]
        public void CancelSpawn_SnapsScaleToOne()
        {
            m_lootable.SetTargetPosition(new Vector3(5f, 5f, 0f));
            m_lootable.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_lootable.StartSpawnSequence(0.5f, 0.01f, 0f);

            m_lootable.CancelSpawn();

            Assert.AreEqual(Vector3.one, m_gameObject.transform.localScale);
        }

        [Test]
        public void CancelSpawn_SnapsPositionToTarget()
        {
            Vector3 target = new Vector3(5f, 5f, 0f);
            m_lootable.SetTargetPosition(target);
            m_lootable.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_lootable.StartSpawnSequence(0.5f, 0.01f, 0f);

            m_lootable.CancelSpawn();

            Assert.AreEqual(target, m_gameObject.transform.position);
        }

        [Test]
        public void CancelSpawn_WhenNotSpawning_DoesNothing()
        {
            Vector3 originalPos = m_gameObject.transform.position;

            m_lootable.CancelSpawn();

            Assert.IsFalse(m_lootable.IsSpawning);
            Assert.AreEqual(originalPos, m_gameObject.transform.position);
        }

        [Test]
        public void BounceEffect_LazyDiscoversComponent()
        {
            // Lazy property should find BounceEffect via GetComponent
            var go = new GameObject("TestAutoDiscover");
            var bounce = go.AddComponent<BounceEffect>();
            bounce.enabled = false;
            var lootable = go.AddComponent<Lootable>();

            // Accessing the property triggers lazy GetComponent lookup
            Assert.AreEqual(bounce, lootable.BounceEffect);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BounceEffect_IsNullWhenNonePresent()
        {
            Assert.IsNull(m_lootable.BounceEffect);
        }
    }
}
