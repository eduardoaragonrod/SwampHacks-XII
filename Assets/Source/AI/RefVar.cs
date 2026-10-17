
/// <summary>
/// Use this to pass references to variables into AI states. This is needed since value types like Vectors and number types are copied not referenced.<br/>
/// Example:<br/>
/// public int MyInt = 5;
/// Becomes
/// public RefVar<int> MyInt = new(5);
/// </summary>
public class RefVar<T>
{
    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            OnChanged?.Invoke(value);
        }
    }
    private T _value;

    public event System.Action<T> OnChanged;

    public RefVar() { }
    /// <summary>
    /// This setting of the intial value will not trigger the OnChanged callback.
    /// </summary>
    public RefVar(T value)
    {
        _value = value;
    }
}
