using NUnit.Framework;
using UnityEngine;
using UnityServiceLocator;

namespace Tests.EditMode
{
    public class AbilityPickupEffectTests
    {
        private GameObject m_playerGO;
        private AbilityController m_controller;
        private GameObject m_pickupGO;
        private AbilityPickupEffect m_pickup;
        private ActiveAbility m_abilityA;
        private ActiveAbility m_abilityB;

        [SetUp]
        public void SetUp()
        {
            m_playerGO = new GameObject("TestPlayer");
            m_controller = m_playerGO.AddComponent<AbilityController>();
            // Awake doesn't fire on AddComponent in edit-mode tests, so register explicitly
            // — at runtime AbilityController.Awake registers itself via the same call.
            ServiceLocator.Global.Register(m_controller);

            m_pickupGO = new GameObject("TestPickup");
            m_pickup = m_pickupGO.AddComponent<AbilityPickupEffect>();

            m_abilityA = ScriptableObject.CreateInstance<ActiveAbility>();
            m_abilityA.Cooldown = 2f;

            m_abilityB = ScriptableObject.CreateInstance<ActiveAbility>();
            m_abilityB.Cooldown = 5f;

            SetPrivateAbility(m_pickup, m_abilityA);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_playerGO);
            Object.DestroyImmediate(m_pickupGO);
            Object.DestroyImmediate(m_abilityA);
            Object.DestroyImmediate(m_abilityB);
            ResetServiceLocatorStatic();
        }

        private static void ResetServiceLocatorStatic()
        {
            var globalField = typeof(ServiceLocator).GetField(
                "_global",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (globalField == null) return;
            ServiceLocator current = globalField.GetValue(null) as ServiceLocator;
            globalField.SetValue(null, null);
            if (current != null) Object.DestroyImmediate(current.gameObject);
        }

        [Test]
        public void Apply_WithControllerRegistered_EquipsAbilityInFirstSlot()
        {
            bool result = m_pickup.Apply(m_playerGO);

            Assert.IsTrue(result);
            Assert.AreSame(m_abilityA, m_controller.GetAbility(0));
        }

        [Test]
        public void Apply_FillsLowestEmptySlot()
        {
            m_controller.Equip(0, m_abilityB);

            bool result = m_pickup.Apply(m_playerGO);

            Assert.IsTrue(result);
            Assert.AreSame(m_abilityB, m_controller.GetAbility(0));
            Assert.AreSame(m_abilityA, m_controller.GetAbility(1));
        }

        [Test]
        public void Apply_WhenAllSlotsFull_ReplacesSlotZero()
        {
            for (int i = 0; i < AbilityController.SlotCount; i++)
                m_controller.Equip(i, m_abilityB);

            bool result = m_pickup.Apply(m_playerGO);

            Assert.IsTrue(result);
            Assert.AreSame(m_abilityA, m_controller.GetAbility(0));
            for (int i = 1; i < AbilityController.SlotCount; i++)
                Assert.AreSame(m_abilityB, m_controller.GetAbility(i));
        }

        [Test]
        public void Apply_WithoutAbilityAssigned_ReturnsFalse()
        {
            SetPrivateAbility(m_pickup, null);

            bool result = m_pickup.Apply(m_playerGO);

            Assert.IsFalse(result);
        }

        // Reach into the SerializeField via reflection so tests don't need the prefab/inspector path.
        private static void SetPrivateAbility(AbilityPickupEffect pickup, ActiveAbility ability)
        {
            var field = typeof(AbilityPickupEffect).GetField(
                "m_ability",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(pickup, ability);
        }
    }
}
