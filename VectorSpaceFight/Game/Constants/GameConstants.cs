using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Constants;

public static class GameConstants
{
    public const int WorldWidth = 1280;
    public const int WorldHeight = 720;

    public const float MatchDurationSeconds = 30f;
    public const float ShieldDuration = 3f;
    public const float ShieldCooldown = 10f;
    public const float SpawnShieldDuration = 3f;
    public const float RespawnDelay = 2f;
    public const float LeaderHighlightDuration = 3f;

    public const float VectorLineIntensity = 1.22f;

    public const float BloomDefaultIntensity = 3f;
    public const float BloomAdjustStep = 0.05f;
    public const float BloomMinIntensity = 0f;
    public const float BloomMaxIntensity = 4f;

    public const float HudScoreScale = 7.5f;
    public const float HudTimerScale = 4f;

    public const float ShipThrust = 200f;
    public const float ShipDrag = 0.99f;
    public const float MaxShipSpeed = 350f;
    public const float RotationSpeed = 4f;
    public const float StickDeadzone = 0.15f;

    public const float BulletSpeed = 500f;
    public const float BulletLifetime = 1.5f;
    public const float FireRate = 0.25f;
    public const int MaxActiveBulletsPerPlayer = 3;

    public const float ShipRadius = 12f;
    public const float BulletRadius = 2f;
    public const float LargeAsteroidRadius = 40f;
    public const float MediumAsteroidRadius = 24f;
    public const float SmallAsteroidRadius = 12f;

    public const int InitialLargeAsteroids = 6;
    public const float AsteroidSpawnInterval = 15f;
    public const float WrapGhostMargin = 60f;

    public static readonly Color[] PlayerColors =
    {
        new(0, 255, 255),
        new(255, 72, 72),
        new(255, 255, 64),
        new(72, 255, 72)
    };

    public static Vector2 GetSpawnPosition(int playerIndex)
    {
        return playerIndex switch
        {
            0 => new Vector2(WorldWidth * 0.15f, WorldHeight * 0.15f),
            1 => new Vector2(WorldWidth * 0.85f, WorldHeight * 0.15f),
            2 => new Vector2(WorldWidth * 0.15f, WorldHeight * 0.85f),
            3 => new Vector2(WorldWidth * 0.85f, WorldHeight * 0.85f),
            _ => new Vector2(WorldWidth * 0.5f, WorldHeight * 0.5f)
        };
    }

    public static Vector2 GetQuadrantScoreAnchor(int playerIndex)
    {
        const float margin = 48f;
        return playerIndex switch
        {
            0 => new Vector2(margin, margin),
            1 => new Vector2(WorldWidth - margin, margin),
            2 => new Vector2(margin, WorldHeight - margin),
            3 => new Vector2(WorldWidth - margin, WorldHeight - margin),
            _ => new Vector2(margin, margin)
        };
    }

    public static float GetAsteroidMass(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => LargeAsteroidRadius * LargeAsteroidRadius,
        AsteroidSize.Medium => MediumAsteroidRadius * MediumAsteroidRadius,
        AsteroidSize.Small => SmallAsteroidRadius * SmallAsteroidRadius,
        _ => SmallAsteroidRadius * SmallAsteroidRadius
    };

    public static float GetAsteroidRadius(AsteroidSize size) => size switch
    {
        AsteroidSize.Large => LargeAsteroidRadius,
        AsteroidSize.Medium => MediumAsteroidRadius,
        AsteroidSize.Small => SmallAsteroidRadius,
        _ => SmallAsteroidRadius
    };
}

public enum AsteroidSize
{
    Large,
    Medium,
    Small
}
