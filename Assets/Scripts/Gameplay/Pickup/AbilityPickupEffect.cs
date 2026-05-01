using UnityEngine;
using UnityServiceLocator;

public class AbilityPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private ActiveAbility m_ability;

    public ActiveAbility Ability => m_ability;

    public bool Apply(GameObject collector)
    {
        if (m_ability == null) return false;
        if (!ServiceLocator.Global.TryGet(out AbilityController controller)) return false;

        controller.Equip(m_ability);
        return true;
    }
}
