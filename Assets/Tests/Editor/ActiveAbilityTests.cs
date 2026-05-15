using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class ActiveAbilityTests
    {
        private const int SlotA = 0;
        private const int SlotB = 1;

        private GameObject m_playerGO;
        private AbilityController m_controller;
        private CountingEffect m_countingEffect;
        private ActiveAbility m_abilityA;
        private ActiveAbility m_abilityB;

        [SetUp]
        public void SetUp()
        {
            m_playerGO = new GameObject("TestPlayer");
            m_controller = m_playerGO.AddComponent<AbilityController>();

            m_countingEffect = new CountingEffect();

            m_abilityA = ScriptableObject.CreateInstance<ActiveAbility>();
            m_abilityA.Cooldown = 2f;
            m_abilityA.Effect = m_countingEffect;

            m_abilityB = ScriptableObject.CreateInstance<ActiveAbility>();
            m_abilityB.Cooldown = 5f;
            m_abilityB.Effect = m_countingEffect;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_playerGO);
            Object.DestroyImmediate(m_abilityA);
            Object.DestroyImmediate(m_abilityB);
        }

        [Test]
        public void Equip_SetsAbilityInSlot()
        {
            int equippedSlot = -1;
            ActiveAbility lastEquipped = null;
            m_controller.OnAbilityEquipped += (slot, a) => { equippedSlot = slot; lastEquipped = a; };

            m_controller.Equip(SlotA, m_abilityA);

            Assert.AreSame(m_abilityA, m_controller.GetAbility(SlotA));
            Assert.AreEqual(SlotA, equippedSlot);
            Assert.AreSame(m_abilityA, lastEquipped);
        }

        [Test]
        public void Equip_StartsReadyEvenIfPreviousAbilityWasOnCooldown()
        {
            m_controller.Equip(SlotA, m_abilityA);
            m_controller.TryUse(SlotA);
            Assert.IsFalse(m_controller.IsReady(SlotA));

            m_controller.Equip(SlotA, m_abilityB);

            Assert.IsTrue(m_controller.IsReady(SlotA));
            Assert.AreEqual(0f, m_controller.GetCooldownRemaining(SlotA));
        }

        [Test]
        public void TryUse_WhileReady_ExecutesEffectAndStartsCooldown()
        {
            m_controller.Equip(SlotA, m_abilityA);

            m_controller.TryUse(SlotA);

            Assert.AreEqual(1, m_countingEffect.Count);
            Assert.IsFalse(m_controller.IsReady(SlotA));
            Assert.AreEqual(m_abilityA.Cooldown, m_controller.GetCooldownRemaining(SlotA));
        }

        [Test]
        public void TryUse_WhileOnCooldown_DoesNotExecute()
        {
            m_controller.Equip(SlotA, m_abilityA);
            m_controller.TryUse(SlotA);

            m_controller.TryUse(SlotA);
            m_controller.TryUse(SlotA);

            Assert.AreEqual(1, m_countingEffect.Count);
        }

        [Test]
        public void TryUse_WithoutEquippedAbility_DoesNothing()
        {
            m_controller.TryUse(SlotA);
            Assert.AreEqual(0, m_countingEffect.Count);
        }

        [Test]
        public void TryUse_OutOfRangeSlot_DoesNothing()
        {
            m_controller.Equip(SlotA, m_abilityA);

            m_controller.TryUse(-1);
            m_controller.TryUse(AbilityController.SlotCount);

            Assert.AreEqual(0, m_countingEffect.Count);
        }

        [Test]
        public void Slots_AreIndependent()
        {
            m_controller.Equip(SlotA, m_abilityA);
            m_controller.Equip(SlotB, m_abilityB);

            m_controller.TryUse(SlotA);

            Assert.IsFalse(m_controller.IsReady(SlotA));
            Assert.IsTrue(m_controller.IsReady(SlotB));
            Assert.AreEqual(m_abilityA.Cooldown, m_controller.GetCooldownRemaining(SlotA));
            Assert.AreEqual(0f, m_controller.GetCooldownRemaining(SlotB));
        }

        [Test]
        public void Tick_DrainsCooldownAndFiresOnCooldownReadyExactlyOnce()
        {
            m_controller.Equip(SlotA, m_abilityA);
            m_controller.TryUse(SlotA);

            int readyEvents = 0;
            int lastReadySlot = -1;
            m_controller.OnCooldownReady += slot => { readyEvents++; lastReadySlot = slot; };

            m_controller.Tick(1f);
            Assert.IsFalse(m_controller.IsReady(SlotA));
            Assert.AreEqual(0, readyEvents);

            m_controller.Tick(1.5f);

            Assert.IsTrue(m_controller.IsReady(SlotA));
            Assert.AreEqual(0f, m_controller.GetCooldownRemaining(SlotA));
            Assert.AreEqual(1, readyEvents);
            Assert.AreEqual(SlotA, lastReadySlot);

            m_controller.Tick(1f);
            Assert.AreEqual(1, readyEvents);
        }

        [Test]
        public void Reset_ClearsAllSlotsAndCooldowns()
        {
            m_controller.Equip(SlotA, m_abilityA);
            m_controller.Equip(SlotB, m_abilityB);
            m_controller.TryUse(SlotA);

            m_controller.Reset();

            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                Assert.IsNull(m_controller.GetAbility(i));
                Assert.IsFalse(m_controller.IsReady(i));
                Assert.AreEqual(0f, m_controller.GetCooldownRemaining(i));
            }
        }

        [Test]
        public void FindFirstEmptySlot_ReturnsLowestUnoccupiedSlot()
        {
            Assert.AreEqual(0, m_controller.FindFirstEmptySlot());

            m_controller.Equip(0, m_abilityA);
            Assert.AreEqual(1, m_controller.FindFirstEmptySlot());

            m_controller.Equip(1, m_abilityB);
            m_controller.Equip(2, m_abilityA);
            m_controller.Equip(3, m_abilityB);
            Assert.AreEqual(-1, m_controller.FindFirstEmptySlot());
        }

        // Lightweight stand-in for IAbilityEffect that just counts invocations, so the
        // controller's gating logic can be exercised without a real effect's dependencies.
        private class CountingEffect : IAbilityEffect
        {
            public int Count;
            public void Execute(in AbilityContext ctx) => Count++;
        }
    }
}
