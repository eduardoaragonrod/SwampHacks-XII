using UnityEngine;

public class Layers : MonoBehaviour
{
    public static string ObstacleName { get; } = "Obstacle";
    public static int ObstacleLayer { get; private set; }
    public static int ObstacleMask { get; private set; }

    public static string EnemyName { get; } = "Enemy";
    public static int EnemyLayer { get; private set; }
    public static int EnemyMask { get; private set; }

    public static string PlayerName { get; } = "Player";
    public static int PlayerLayer { get; private set; }
    public static int PlayerMask { get; private set; }

    public static int CharacterMask { get; private set; }

    private void Awake()
    {
        ObstacleLayer = LayerMask.NameToLayer(ObstacleName);
        ObstacleMask = LayerMask.GetMask(ObstacleName);

        EnemyLayer = LayerMask.NameToLayer(EnemyName);
        EnemyMask = LayerMask.GetMask(EnemyName);

        PlayerLayer = LayerMask.NameToLayer(PlayerName);
        PlayerMask = LayerMask.GetMask(PlayerName);

        CharacterMask = LayerMask.GetMask(PlayerName, EnemyName);
    }
}
