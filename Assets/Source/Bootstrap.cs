using UnityEngine;
using UnityEngine.AddressableAssets;

public class Bootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Launch()
    {
        var instance = Addressables.InstantiateAsync("Globals.prefab").WaitForCompletion();
        instance.name = "Globals";
        DontDestroyOnLoad(instance);
    }
}
