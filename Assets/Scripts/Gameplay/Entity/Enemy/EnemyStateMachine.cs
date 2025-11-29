using System;
using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    private IState _currentState;

    public Action<IState> OnStateChanged;

    private void ChangeState(IState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
        OnStateChanged?.Invoke(_currentState);
    }

    public void Initialize(IState initialState)
    {
        ChangeState(initialState);
    }

    private void Update()
    {
        _currentState.Tick();
    }
    
    private void OnDestroy()
    {
        _currentState.Exit();
    }
}