using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class PlayerStatusEffectsTests
    {
        private GameObject m_playerGO;
        private PlayerStatusEffects m_status;
        private EntityHealth m_health;

        [SetUp]
        public void SetUp()
        {
            m_playerGO = new GameObject("TestPlayer");
            m_health = m_playerGO.AddComponent<EntityHealth>();
            m_status = m_playerGO.AddComponent<PlayerStatusEffects>();

            // Wire the serialized health reference via reflection — avoids needing a prefab.
            var field = typeof(PlayerStatusEffects).GetField(
                "m_health",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(m_status, m_health);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_playerGO);
        }

        [Test]
        public void GetMultiplier_DefaultsToOne_WhenInactive()
        {
            Assert.AreEqual(1f, m_status.GetMultiplier(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.AreEqual(1f, m_status.GetMultiplier(PlayerStatusEffects.BuffKind.SpeedMultiplier));
        }

        [Test]
        public void ApplyTimed_ActivatesBuff_AndReturnsMagnitudeFromGetMultiplier()
        {
            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.DamageMultiplier, 1.5f, 3f);

            Assert.IsTrue(m_status.IsActive(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.AreEqual(1.5f, m_status.GetMultiplier(PlayerStatusEffects.BuffKind.DamageMultiplier));
        }

        [Test]
        public void ApplyTimed_FiresOnBuffStartedExactlyOnceUntilExpiry()
        {
            int starts = 0;
            m_status.OnBuffStarted += (k, _) => { if (k == PlayerStatusEffects.BuffKind.SpeedMultiplier) starts++; };

            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.SpeedMultiplier, 1.5f, 2f);
            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.SpeedMultiplier, 1.5f, 2f); // refresh, not a new start

            Assert.AreEqual(1, starts);
        }

        [Test]
        public void Tick_ExpiresBuff_FiresOnBuffEnded_AndResetsMultiplier()
        {
            int ends = 0;
            m_status.OnBuffEnded += k => { if (k == PlayerStatusEffects.BuffKind.DamageMultiplier) ends++; };

            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.DamageMultiplier, 2f, 1f);

            m_status.Tick(0.5f);
            Assert.IsTrue(m_status.IsActive(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.AreEqual(0, ends);

            m_status.Tick(0.6f);
            Assert.IsFalse(m_status.IsActive(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.AreEqual(1f, m_status.GetMultiplier(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.AreEqual(1, ends);
        }

        [Test]
        public void Invulnerable_TogglesEntityHealthIsGodMode()
        {
            Assert.IsFalse(m_health.IsGodMode);

            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.Invulnerable, 1f, 1f);
            Assert.IsTrue(m_health.IsGodMode);

            m_status.Tick(2f);
            Assert.IsFalse(m_health.IsGodMode);
        }

        [Test]
        public void Clear_DropsAllBuffsAndClearsGodMode()
        {
            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.Invulnerable, 1f, 5f);
            m_status.ApplyTimed(PlayerStatusEffects.BuffKind.DamageMultiplier, 2f, 5f);

            m_status.Clear();

            Assert.IsFalse(m_status.IsActive(PlayerStatusEffects.BuffKind.Invulnerable));
            Assert.IsFalse(m_status.IsActive(PlayerStatusEffects.BuffKind.DamageMultiplier));
            Assert.IsFalse(m_health.IsGodMode);
        }
    }
}
