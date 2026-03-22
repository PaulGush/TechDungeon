using UnityEngine;
using UnityServiceLocator;

public class MutationPickup : Lootable
{
    [Header("Mutation")]
    [SerializeField] private Mutation m_mutation;

    public Mutation Mutation => m_mutation;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsSpawning) return;
        if (other.gameObject.layer != GameConstants.Layers.PlayerLayer) return;

        if (ServiceLocator.Global.TryGet(out MutationManager mutationManager))
        {
            mutationManager.AddMutation(m_mutation);
        }

        Destroy(gameObject);
    }
}
