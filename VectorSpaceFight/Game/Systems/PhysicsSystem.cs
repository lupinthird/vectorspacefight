using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Audio;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.Systems;

public static class PhysicsSystem
{
    public static ShipUpdateResult ApplyShipControl(Ship ship, PlayerInputState input, float dt, List<Bullet> bullets)
    {
        if (!ship.IsAlive)
            return default;

        bool firedShot = false;

        if (input.UseAbsoluteHeading)
            ship.Rotation = WrapAngle(input.AbsoluteHeading);
        else if (input.Rotate != 0f)
            ship.Rotation += input.Rotate * GameConstants.RotationSpeed * dt;

        if (input.Thrust)
            ship.Velocity += ship.Facing * GameConstants.ShipThrust * dt;

        ship.FireCooldown -= dt;

        bool shieldUp = ship.ShieldActive;
        int bulletLimit = shieldUp
            ? GameConstants.MaxActiveBulletsWithShield
            : GameConstants.MaxActiveBulletsPerPlayer;

        if (input.Shoot && ship.FireCooldown <= 0f &&
            CountActiveBullets(bullets, ship.PlayerIndex) < bulletLimit)
        {
            var bullet = GetInactiveBullet(bullets);
            var muzzle = ship.Position + ship.Facing * 16f;
            bullet.Spawn(muzzle, ship.Facing, ship.PlayerIndex);
            ship.FireCooldown = GameConstants.FireRate;
            firedShot = true;

            if (shieldUp)
            {
                ship.ShieldSuppressed = true;
                ship.ShieldBreachTimer = GameConstants.ShieldBreachMinDuration;
                ship.ShieldBreachBullet = bullet;
            }
        }

        return new ShipUpdateResult { FiredShot = firedShot };
    }

    public static void IntegrateShips(Ship[] ships, float dt)
    {
        foreach (var ship in ships)
        {
            if (!ship.IsAlive)
                continue;

            ship.Velocity *= GameConstants.ShipDrag;

            if (ship.Velocity.LengthSquared() > GameConstants.MaxShipSpeed * GameConstants.MaxShipSpeed)
            {
                ship.Velocity.Normalize();
                ship.Velocity *= GameConstants.MaxShipSpeed;
            }

            ship.Position += ship.Velocity * dt;
        }
    }

    public static void UpdateShieldBreaches(Ship[] ships, float dt)
    {
        float shieldRadiusSq = GameConstants.ShieldRadius * GameConstants.ShieldRadius;

        foreach (var ship in ships)
        {
            if (!ship.ShieldSuppressed)
                continue;

            ship.ShieldBreachTimer -= dt;

            var breachBullet = ship.ShieldBreachBullet;
            bool bulletClear = breachBullet == null || !breachBullet.Active ||
                               Vector2.DistanceSquared(ship.Position, breachBullet.Position) > shieldRadiusSq;

            if (ship.ShieldBreachTimer <= 0f && bulletClear)
            {
                ship.ShieldSuppressed = false;
                ship.ShieldBreachTimer = 0f;
                ship.ShieldBreachBullet = null;
            }
        }
    }

    public static void UpdateBullets(List<Bullet> bullets, float dt)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            bullet.Lifetime -= dt;
            if (bullet.Lifetime <= 0f)
            {
                bullet.Active = false;
                continue;
            }

            bullet.Position += bullet.Velocity * dt;
        }
    }

    public static void UpdateAsteroids(List<Asteroid> asteroids, float dt)
    {
        foreach (var asteroid in asteroids)
        {
            if (!asteroid.Active)
                continue;

            asteroid.Position += asteroid.Velocity * dt;
            asteroid.Rotation += asteroid.RotationSpeed * dt;
        }
    }

    private static int CountActiveBullets(List<Bullet> bullets, int ownerIndex)
    {
        int count = 0;
        foreach (var bullet in bullets)
        {
            if (bullet.Active && bullet.OwnerIndex == ownerIndex)
                count++;
        }

        return count;
    }

    private static Bullet GetInactiveBullet(List<Bullet> bullets)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                return bullet;
        }

        var created = new Bullet();
        bullets.Add(created);
        return created;
    }

    private static float WrapAngle(float radians)
    {
        radians %= MathF.Tau;
        if (radians < 0f)
            radians += MathF.Tau;

        return radians;
    }
}

