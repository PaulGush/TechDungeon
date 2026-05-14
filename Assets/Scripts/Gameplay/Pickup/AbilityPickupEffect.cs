using UnityEngine;
using UnityServiceLocator;

public class AbilityPickupEffect : MonoBehaviour, IPickupEffect, IPickupTooltip
{
    [SerializeField] private ActiveAbility m_ability;

    public ActiveAbility Ability => m_ability;

    public bool Apply(GameObject collector)
    {
        if (m_ability == null) return false;
        if (!ServiceLocator.Global.TryGet(out AbilityController controller)) return false;

        // Fill the first open slot; if all four are taken, replace slot 0 so a pickup is never wasted.
        int slot = controller.FindFirstEmptySlot();
        if (slot < 0) slot = 0;
        controller.Equip(slot, m_ability);
        return true;
    }

    public bool TryGetTooltip(out string title, out string body, out string effect)
    {
        if (m_ability == null)
        {
            title = body = effect = null;
            return false;
        }

        title = m_ability.DisplayName;
        body = m_ability.Description;
        effect = $"Cooldown {m_ability.Cooldown:0.#}s";
        return true;
    }
}
