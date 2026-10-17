public record MissilePattern
{
    public bool RandomSpread;
    public float SpreadAngle = 45f;

    public static MissilePattern StraightShot { get; } = new()
    {
        RandomSpread = false,
        SpreadAngle = 0f
    };

    public static MissilePattern OmniExplosion { get; } = new()
    {
        RandomSpread = true,
        SpreadAngle = 360f
    };
}
