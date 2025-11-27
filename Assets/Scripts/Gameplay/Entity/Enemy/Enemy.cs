using UnityEngine;

public class Enemy : Entity
{
    [SerializeField] private EnemyStateMachine _stateMachine;
    
    private void Start()
    {
        _stateMachine = new EnemyStateMachine();
        _stateMachine.Initialize(new IdleState());
    }
}