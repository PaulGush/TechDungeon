using System.Collections.Generic;
using UnityEngine.Playables;

public class BossEventMixerBehaviour : PlayableBehaviour
{
    private BossDeathTrigger m_trigger;
    private readonly HashSet<int> m_firedEvents = new HashSet<int>();
    private readonly List<BossEventType> m_allEvents = new List<BossEventType>();
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
                m_allEvents.Add(input.GetBehaviour().EventType);
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

            FireEvent(behaviour.EventType);
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
            FireEvent(m_allEvents[i]);
        }
    }

    private void FireEvent(BossEventType eventType)
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
        }
    }
}
