using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Audio;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.Systems;

public static class PhysicsSystem
{
    public static ShipUpdateResult UpdateShip(Ship ship, PlayerInputState input, float dt, List<Bullet> bullets)
    {
        if (!ship.IsAlive)
            return default;

        bool firedShot = false;

        if (input.Rotate != 0f)
            ship.Rotation += input.Rotate * GameConstants.RotationSpeed * dt;

        if (input.Thrust)
            ship.Velocity += ship.Facing * GameConstants.ShipThrust * dt;

        ship.Velocity *= GameConstants.ShipDrag;

        if (ship.Velocity.LengthSquared() > GameConstants.MaxShipSpeed * GameConstants.MaxShipSpeed)
        {
            ship.Velocity.Normalize();
            ship.Velocity *= GameConstants.MaxShipSpeed;
        }

        ship.Position += ship.Velocity * dt;

        ship.FireCooldown -= dt;
        if (input.Shoot && ship.FireCooldown <= 0f)
        {
            var bullet = GetInactiveBullet(bullets);
            var muzzle = ship.Position + ship.Facing * 16f;
            bullet.Spawn(muzzle, ship.Facing, ship.PlayerIndex);
            ship.FireCooldown = GameConstants.FireRate;
            firedShot = true;
        }

        return new ShipUpdateResult { FiredShot = firedShot };
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
}
