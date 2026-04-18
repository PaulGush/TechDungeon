using System.Collections.Generic;
using UnityEngine.Playables;
using UnityServiceLocator;

public class BossEventMixerBehaviour : PlayableBehaviour
{
    private BossDeathTrigger m_trigger;
    private readonly HashSet<int> m_firedEvents = new HashSet<int>();
    private readonly List<CapturedEvent> m_allEvents = new List<CapturedEvent>();
    private bool m_eventsCaptured;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_trigger = playerData as BossDeathTrigger;
        if (m_trigger == null) return;

        int inputCount = playable.GetInputCount();

        // Capture the ordered list of all events on first frame so OnPlayableDestroy
        // can fire any that were skipped.
        if (!m_eventsCaptured)
        {
            for (int i = 0; i < inputCount; i++)
            {
                var input = (ScriptPlayable<BossEventBehaviour>)playable.GetInput(i);
                var b = input.GetBehaviour();
                m_allEvents.Add(new CapturedEvent(b.EventType, b.ShakeAmplitude));
            }
            m_eventsCaptured = true;
        }

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;
            if (m_firedEvents.Contains(i)) continue;

            var input = (ScriptPlayable<BossEventBehaviour>)playable.GetInput(i);
            var behaviour = input.GetBehaviour();

            FireEvent(behaviour.EventType, behaviour.ShakeAmplitude);
            m_firedEvents.Add(i);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        // On skip: fire every event that hasn't fired yet, in timeline order,
        // so the boss always dies and returns to pool.
        if (m_trigger == null) return;

        for (int i = 0; i < m_allEvents.Count; i++)
        {
            if (m_firedEvents.Contains(i)) continue;
            FireEvent(m_allEvents[i].Type, m_allEvents[i].ShakeAmplitude);
        }
    }

    private void FireEvent(BossEventType eventType, float shakeAmplitude)
    {
        switch (eventType)
        {
            case BossEventType.PlayDeathAnimation:
                m_trigger.PlayDeathAnimation();
                break;
            case BossEventType.Kill:
                m_trigger.Kill();
                break;
            case BossEventType.ReturnToPool:
                m_trigger.ReturnToPool();
                break;
            case BossEventType.ScreenShake:
                if (ServiceLocator.Global.TryGet(out CameraShake shake))
                    shake.Shake(shakeAmplitude);
                break;
        }
    }

    private readonly struct CapturedEvent
    {
        public readonly BossEventType Type;
        public readonly float ShakeAmplitude;

        public CapturedEvent(BossEventType type, float shakeAmplitude)
        {
            Type = type;
            ShakeAmplitude = shakeAmplitude;
        }
    }
}
