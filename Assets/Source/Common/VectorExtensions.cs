using UnityEngine;

public static class VectorExtensions
{
    public static float DistanceTo(this Vector2 origin, Vector2 destination)
    {
        return Vector2.Distance(origin, destination);
    }

    public static Vector2 DirectionTo(this Vector2 origin, Vector2 target)
    {
        return (target - origin).normalized;
    }
}
