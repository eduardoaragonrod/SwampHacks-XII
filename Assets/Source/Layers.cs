using UnityEngine;

public class Layers : MonoBehaviour
{
    public static string ObstacleName { get; } = "Obstacle";
    public int ObstacleLayer { get; private set; }
    public static int ObstacleMask { get; private set; }

    public static string EnemyName { get; } = "Enemy";
    public int EnemyLayer { get; private set; }
    public static int EnemyMask { get; private set; }

    public static string PlayerName { get; } = "Player";
    public int PlayerLayer { get; private set; }
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
