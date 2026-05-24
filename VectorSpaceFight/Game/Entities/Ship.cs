using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Entities;

public class Ship
{
    public int PlayerIndex { get; }
    public Color Color { get; }
    public Vector2 SpawnPosition { get; }

    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public int Kills;

    public bool IsAlive = true;
    public bool ShieldActive;
    public bool IsSpawnProtection;
    public float ShieldActiveTimer;
    public float ShieldCooldownTimer;
    public float RespawnTimer;
    public float FireCooldown;
    public float LeaderHighlightTimer;
    public bool IsThrusting;
    public bool ShieldSuppressed;
    public float ShieldBreachTimer;
    public Bullet? ShieldBreachBullet;

    public Ship(int playerIndex)
    {
        PlayerIndex = playerIndex;
        Color = GameConstants.PlayerColors[playerIndex];
        SpawnPosition = GameConstants.GetSpawnPosition(playerIndex);
        ResetToSpawn();
    }

    public void ResetToSpawn()
    {
        Position = SpawnPosition;
        Velocity = Vector2.Zero;
        Rotation = 0f;
        IsAlive = true;
        ShieldActive = false;
        IsSpawnProtection = false;
        ShieldActiveTimer = 0f;
        ShieldCooldownTimer = 0f;
        RespawnTimer = 0f;
        FireCooldown = 0f;
        LeaderHighlightTimer = 0f;
        IsThrusting = false;
        ShieldSuppressed = false;
        ShieldBreachTimer = 0f;
        ShieldBreachBullet = null;
    }

    public Vector2 Facing => new(MathF.Sin(Rotation), -MathF.Cos(Rotation));

    public bool HasShieldProtection => ShieldActive && !ShieldSuppressed;

    public bool IsInvulnerable => HasShieldProtection;

    public void Kill(int killerPlayerIndex, Ship[] ships)
    {
        if (!IsAlive || IsInvulnerable)
            return;

        IsAlive = false;
        RespawnTimer = GameConstants.RespawnDelay;
        Velocity = Vector2.Zero;

        if (killerPlayerIndex >= 0 && killerPlayerIndex < ships.Length && killerPlayerIndex != PlayerIndex)
        {
            ships[killerPlayerIndex].Kills++;
            return;
        }

        if (killerPlayerIndex == -1 && Kills > 0)
            Kills--;
    }
}
