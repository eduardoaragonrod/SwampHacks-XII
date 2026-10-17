using Unity.Scripting.LifecycleManagement;
using UnityEngine;

public partial class ElementManager : MonoBehaviour
{
    [SerializeField]
    private ElementSO Fire, Lightning, Ice;

    private static ElementManager _instance;

    [AutoStaticsCleanup]
    public static event System.Action<Element> OnChanged;

    // HACK: Avoid starting with current element, so player receives initial on element changed event.
    public static Element Current { get; private set; } = (Element)10000;

    private void Awake()
    {
        _instance = this;
    }

    public static void SetElement(Element element)
    {
        if (Current != element)
        {
            Current = element;
            OnChanged?.Invoke(Current);
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
