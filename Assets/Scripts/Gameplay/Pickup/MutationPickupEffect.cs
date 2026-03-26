using UnityEngine;
using UnityServiceLocator;

public class MutationPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private Mutation m_mutation;

    public Mutation Mutation => m_mutation;

    public bool Apply(GameObject collector)
    {
        if (!ServiceLocator.Global.TryGet(out MutationManager mutationManager))
            return false;

        mutationManager.AddMutation(m_mutation);
        return true;
    }
}
