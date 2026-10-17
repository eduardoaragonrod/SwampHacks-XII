using System;
using System.Collections.Generic;

public class StateData
{
    public State State;
    public readonly List<(Func<bool> Condition, State Target)> ExitConditions = new();
    
    public StateData AddExit(Func<bool> condition, State target)
    {
        ExitConditions.Add((condition, target));
        return this;
    }
    public StateData AddExit(State target)
    {
        ExitConditions.Add((() => true, target));
        return this;
    }
}
