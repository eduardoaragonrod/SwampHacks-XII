using UnityEngine;

public class ElementManager : MonoBehaviour
{
    private static ElementManager _instance;

    public static Element Current { get; private set; } = Element.Fire;

    private void Awake()
    {
        _instance = this;
    }

    public static void SetElement(Element element)
    {
        Current = element;
    }
}
