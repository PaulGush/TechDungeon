using NUnit.Framework;
using PlayerObject;
using UnityEngine;

namespace Tests.EditMode
{
    public class WeaponTests
    {
        private GameObject m_weaponGO;
        private Weapon m_weapon;
        private GameObject m_holderGO;
        private WeaponHolder m_holder;

        [SetUp]
        public void SetUp()
        {
            m_holderGO = new GameObject("TestHolder");
            m_holder = m_holderGO.AddComponent<WeaponHolder>();

            m_weaponGO = new GameObject("TestWeapon");
            m_weaponGO.transform.SetParent(m_holderGO.transform);
            m_weapon = m_weaponGO.AddComponent<Weapon>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_holderGO);
        }

        [Test]
        public void Equip_WhenSpawning_CancelsSpawn()
        {
            m_weapon.SetTargetPosition(new Vector3(3f, 3f, 0f));
            m_weapon.SetAboveChestTargetPosition(Vector3.up * 2f);
            m_weapon.StartSpawnSequence(0.5f, 0.01f, 0f);

            Assert.IsTrue(m_weapon.IsSpawning);

            m_weapon.Equip();

            Assert.IsFalse(m_weapon.IsSpawning);
        }

        [Test]
        public void Equip_StopsBounceEffect()
        {
            var bounce = m_weaponGO.AddComponent<BounceEffect>();
            bounce.SetTargets();
            bounce.enabled = true;

            m_weapon.Equip();

            Assert.IsFalse(bounce.enabled);
        }

        [Test]
        public void Unequip_EnablesBounceEffect()
        {
            var bounce = m_weaponGO.AddComponent<BounceEffect>();
            bounce.enabled = false;

            m_weapon.Equip();
            m_weaponGO.transform.SetParent(null);
            m_weapon.Unequip();

            Assert.IsTrue(bounce.enabled);
        }
    }
}
