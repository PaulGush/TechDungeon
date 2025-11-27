using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    private IState _currentState;

    public void ChangeState(IState newState)
    {
        _currentState.Exit();
        _currentState = newState;
        _currentState.Enter();
    }
    
    public void Initialize(IState initialState) => ChangeState(initialState);
    
    private void Update()
    {
        _currentState.Tick();
    }
    
    private void OnDestroy()
    {
        _currentState.Exit();
    }
}