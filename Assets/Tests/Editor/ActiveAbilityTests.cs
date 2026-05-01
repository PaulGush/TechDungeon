using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class ActiveAbilityTests
    {
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
        public void Equip_SetsCurrent()
        {
            ActiveAbility lastEquipped = null;
            m_controller.OnAbilityEquipped += a => lastEquipped = a;

            m_controller.Equip(m_abilityA);

            Assert.AreSame(m_abilityA, m_controller.Current);
            Assert.AreSame(m_abilityA, lastEquipped);
        }

        [Test]
        public void Equip_StartsReadyEvenIfPreviousAbilityWasOnCooldown()
        {
            m_controller.Equip(m_abilityA);
            m_controller.TryUse();
            Assert.IsFalse(m_controller.IsReady);

            m_controller.Equip(m_abilityB);

            Assert.IsTrue(m_controller.IsReady);
            Assert.AreEqual(0f, m_controller.CooldownRemaining);
        }

        [Test]
        public void TryUse_WhileReady_ExecutesEffectAndStartsCooldown()
        {
            m_controller.Equip(m_abilityA);

            m_controller.TryUse();

            Assert.AreEqual(1, m_countingEffect.Count);
            Assert.IsFalse(m_controller.IsReady);
            Assert.AreEqual(m_abilityA.Cooldown, m_controller.CooldownRemaining);
        }

        [Test]
        public void TryUse_WhileOnCooldown_DoesNotExecute()
        {
            m_controller.Equip(m_abilityA);
            m_controller.TryUse();

            m_controller.TryUse();
            m_controller.TryUse();

            Assert.AreEqual(1, m_countingEffect.Count);
        }

        [Test]
        public void TryUse_WithoutEquippedAbility_DoesNothing()
        {
            m_controller.TryUse();
            Assert.AreEqual(0, m_countingEffect.Count);
        }

        [Test]
        public void Tick_DrainsCooldownAndFiresOnCooldownReadyExactlyOnce()
        {
            m_controller.Equip(m_abilityA);
            m_controller.TryUse();

            int readyEvents = 0;
            m_controller.OnCooldownReady += () => readyEvents++;

            // Drain across multiple ticks
            m_controller.Tick(1f);
            Assert.IsFalse(m_controller.IsReady);
            Assert.AreEqual(0, readyEvents);

            m_controller.Tick(1.5f);

            Assert.IsTrue(m_controller.IsReady);
            Assert.AreEqual(0f, m_controller.CooldownRemaining);
            Assert.AreEqual(1, readyEvents);

            // Further ticks while ready should not re-fire the event
            m_controller.Tick(1f);
            Assert.AreEqual(1, readyEvents);
        }

        [Test]
        public void Reset_ClearsCurrentAndCooldown()
        {
            m_controller.Equip(m_abilityA);
            m_controller.TryUse();

            m_controller.Reset();

            Assert.IsNull(m_controller.Current);
            Assert.IsFalse(m_controller.IsReady);
            Assert.AreEqual(0f, m_controller.CooldownRemaining);
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
