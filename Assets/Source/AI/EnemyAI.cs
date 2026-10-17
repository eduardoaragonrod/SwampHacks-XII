using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(Health))]
public abstract class EnemyAI : MonoBehaviour
{
    public CharacterMovement Movement { get; private set; }
    public Health Health { get; private set; }

    protected State Running { get; private set; }

    private readonly Dictionary<State, StateData> _stateData = new();
    private readonly List<(Func<bool> Condition, State Target)> _globalTransitions = new();

    protected virtual void Awake()
    {
        Movement = GetComponent<CharacterMovement>();
        Health = GetComponent<Health>();
    }

    protected virtual void Start()
    {
        Running = OnConstruct();
    }

    private void Update()
    {
        foreach (var (Condition, Target) in _globalTransitions)
        {
            if (Condition())
            {
                TransitionTo(Target);
                break;
            }
        }

        if (Running != null && _stateData.TryGetValue(Running, out var stateData))
        {
            // Call update on running state, but also check if it wants to exit.
            if (Running.OnUpdate())
            {
                foreach (var (Condition, Target) in stateData.ExitConditions)
                {
                    if (Condition())
                    {
                        TransitionTo(Target);
                        break;
                    }
                }
            }
        }
        else
        {
            Running?.OnUpdate();
        }
    }

    protected virtual State OnConstruct() => null;

    protected StateData Register(State state)
    {
        state.SetOwner(this);
        var stateData = new StateData() { State = state };
        _stateData.Add(state, stateData);
        return stateData;
    }

    protected void AddGlobalTransition(Func<bool> condition, State target)
    {
        _globalTransitions.Add((condition, target));
    }

    private void TransitionTo(State state)
    {
        if (state != Running)
        {
            Running?.OnExit();
            Running = state;
            Running?.OnEnter();
        }
    }
}
