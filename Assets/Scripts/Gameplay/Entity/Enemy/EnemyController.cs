using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;
using Object = UnityEngine.Object;

public class EnemyController : Entity
{
    private EnemyStateMachine _stateMachine;
    public EnemyStateMachine StateMachine => _stateMachine;

    [Header("Registered Services")] 
    [SerializeField] private List<Object> _services;

    private void Awake()
    {
        ServiceLocator serviceLocator = ServiceLocator.For(this);
        foreach (var service in _services)
        {
            serviceLocator.Register(service.GetType(), service);
        }
    }

    private void Start()
    {
        _stateMachine = new EnemyStateMachine(this);
        _stateMachine?.Initialize();
    }

    private void Update()
    {
        _stateMachine?.Tick();
    }

    public Object GetService(object serviceType)
    {
        return _services.Find(service => service.GetType() == (Type)serviceType);
    }
}