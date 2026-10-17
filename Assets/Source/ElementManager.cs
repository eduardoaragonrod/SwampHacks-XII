using Unity.Scripting.LifecycleManagement;
using UnityEngine;

public partial class ElementManager : MonoBehaviour
{
    [SerializeField]
    private ElementSO Fire, Lightning, Ice;

    private static ElementManager _instance;

    [AutoStaticsCleanup]
    public static event System.Action<Element> OnChanged;

    [AutoStaticsCleanup]
    public static Element? Current { get; private set; }
    public static ElementSO CurrentData
    {
        get
        {
            if (Current.HasValue)
            {
                return GetData(Current.Value);
            }

            throw new System.NotImplementedException("Invalid current element type! Not implemented yet?");
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    public static void SetElement(Element element)
    {
        if (Current != element)
        {
            Current = element;
            OnChanged?.Invoke(Current.Value);
        }
    }

    public static ElementSO GetData(Element element) => element switch
    {
        Element.Fire => _instance.Fire,
        Element.Lightning => _instance.Lightning,
        Element.Ice => _instance.Ice,
        _ => throw new System.NotImplementedException(),
    };
}
