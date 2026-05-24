using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.Systems;

public class CollisionSystem
{
    private readonly AsteroidSpawner _asteroidSpawner;
    private readonly DebrisSystem _debrisSystem;
    private readonly List<LineDebris> _debris;
    private readonly Action? _playRumble;

    public CollisionSystem(AsteroidSpawner asteroidSpawner, DebrisSystem debrisSystem, List<LineDebris> debris,
        Action? playRumble = null)
    {
        _asteroidSpawner = asteroidSpawner;
        _debrisSystem = debrisSystem;
        _debris = debris;
        _playRumble = playRumble;
    }

    public void Update(Ship[] ships, List<Bullet> bullets, List<Asteroid> asteroids)
    {
        ResolveAsteroidAsteroid(asteroids);
        ResolveBulletAsteroid(bullets, asteroids);
        ResolveBulletShip(ships, bullets);
        ResolveShipShip(ships);
        ResolveShipAsteroid(ships, asteroids);
        ResolveShieldAsteroid(ships, asteroids);
    }

    private void ResolveAsteroidAsteroid(List<Asteroid> asteroids)
    {
        const float restitution = 0.88f;

        for (int i = 0; i < asteroids.Count; i++)
        {
            var a = asteroids[i];
            if (!a.Active)
                continue;

            for (int j = i + 1; j < asteroids.Count; j++)
            {
                var b = asteroids[j];
                if (!b.Active)
                    continue;

                var delta = b.Position - a.Position;
                float minDist = a.Radius + b.Radius;
                float distSq = delta.LengthSquared();
                if (distSq >= minDist * minDist)
                    continue;

                float dist = MathF.Sqrt(distSq);
                var normal = dist > 0.001f ? delta / dist : RandomUnitNormal();

                float overlap = minDist - dist;
                float massA = GameConstants.GetAsteroidMass(a.Size);
                float massB = GameConstants.GetAsteroidMass(b.Size);
                float totalMass = massA + massB;
                a.Position -= normal * overlap * (massB / totalMass);
                b.Position += normal * overlap * (massA / totalMass);

                var relativeVelocity = b.Velocity - a.Velocity;
                float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
                if (velocityAlongNormal > 0f)
                    continue;

                float impulse = -(1f + restitution) * velocityAlongNormal / (1f / massA + 1f / massB);
                a.Velocity -= normal * impulse / massA;
                b.Velocity += normal * impulse / massB;
            }
        }
    }

    private static Vector2 RandomUnitNormal()
    {
        float angle = (float)(Random.Shared.NextDouble() * MathF.Tau);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    private void ResolveBulletAsteroid(List<Bullet> bullets, List<Asteroid> asteroids)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            Asteroid? hit = null;
            foreach (var asteroid in asteroids)
            {
                if (!asteroid.Active)
                    continue;

                if (!CirclesOverlap(bullet.Position, GameConstants.BulletRadius, asteroid.Position, asteroid.Radius))
                    continue;

                hit = asteroid;
                break;
            }

            if (hit == null)
                continue;

            bullet.Active = false;
            BreakAsteroid(asteroids, hit, bullet.Velocity);
        }
    }

    private void ResolveBulletShip(Ship[] ships, List<Bullet> bullets)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            foreach (var ship in ships)
            {
                if (!ship.IsAlive || ship.PlayerIndex == bullet.OwnerIndex || ship.IsInvulnerable)
                    continue;

                if (!CirclesOverlap(bullet.Position, GameConstants.BulletRadius, ship.Position, GameConstants.ShipRadius))
                    continue;

                bullet.Active = false;
                _debrisSystem.SpawnShipDebris(_debris, ship, bullet.Velocity);
                ship.Kill(bullet.OwnerIndex, ships);
                _playRumble?.Invoke();
                break;
            }
        }
    }

    private void ResolveShipShip(Ship[] ships)
    {
        float mass = GameConstants.ShipMass;
        float restitution = GameConstants.ShipShipRestitution;
        float invMassSum = 2f / mass;

        for (int i = 0; i < ships.Length; i++)
        {
            var a = ships[i];
            if (!a.IsAlive)
                continue;

            for (int j = i + 1; j < ships.Length; j++)
            {
                var b = ships[j];
                if (!b.IsAlive)
                    continue;

                float radiusA = GetShipCollisionRadius(a);
                float radiusB = GetShipCollisionRadius(b);
                var delta = b.Position - a.Position;
                float minDist = radiusA + radiusB;
                float distSq = delta.LengthSquared();
                if (distSq >= minDist * minDist)
                    continue;

                float dist = MathF.Sqrt(distSq);
                var normal = dist > 0.001f ? delta / dist : RandomUnitNormal();

                bool aShield = a.HasShieldProtection;
                bool bShield = b.HasShieldProtection;

                if (aShield && !bShield)
                {
                    _debrisSystem.SpawnShipDebris(_debris, b, a.Velocity - b.Velocity);
                    b.Kill(a.PlayerIndex, ships);
                    _playRumble?.Invoke();
                    continue;
                }

                if (bShield && !aShield)
                {
                    _debrisSystem.SpawnShipDebris(_debris, a, b.Velocity - a.Velocity);
                    a.Kill(b.PlayerIndex, ships);
                    _playRumble?.Invoke();
                    continue;
                }

                float overlap = minDist - dist;
                a.Position -= normal * overlap * 0.5f;
                b.Position += normal * overlap * 0.5f;

                float velocityAlongNormal = Vector2.Dot(b.Velocity - a.Velocity, normal);
                if (velocityAlongNormal > 0f)
                    continue;

                float impulse = -(1f + restitution) * velocityAlongNormal / invMassSum;
                a.Velocity -= normal * (impulse / mass);
                b.Velocity += normal * (impulse / mass);
            }
        }
    }

    private static float GetShipCollisionRadius(Ship ship) =>
        ship.HasShieldProtection ? GameConstants.ShieldRadius : GameConstants.ShipRadius;

    private void ResolveShipAsteroid(Ship[] ships, List<Asteroid> asteroids)
    {
        foreach (var ship in ships)
        {
            if (!ship.IsAlive || ship.HasShieldProtection)
                continue;

            foreach (var asteroid in asteroids)
            {
                if (!asteroid.Active)
                    continue;

                if (!CirclesOverlap(ship.Position, GameConstants.ShipRadius, asteroid.Position, asteroid.Radius))
                    continue;

                _debrisSystem.SpawnShipDebris(_debris, ship, asteroid.Velocity);
                ship.Kill(-1, ships);
                _playRumble?.Invoke();
                break;
            }
        }
    }

    private void ResolveShieldAsteroid(Ship[] ships, List<Asteroid> asteroids)
    {
        foreach (var ship in ships)
        {
            if (!ship.IsAlive || !ship.HasShieldProtection)
                continue;

            Asteroid? hit = null;
            foreach (var asteroid in asteroids)
            {
                if (!asteroid.Active)
                    continue;

                if (!CirclesOverlap(ship.Position, GameConstants.ShieldRadius, asteroid.Position, asteroid.Radius))
                    continue;

                hit = asteroid;
                break;
            }

            if (hit == null)
                continue;

            var impact = ship.Velocity.LengthSquared() > 1f ? ship.Velocity : ship.Facing * 100f;
            BreakAsteroid(asteroids, hit, impact);
        }
    }

    private void BreakAsteroid(List<Asteroid> asteroids, Asteroid asteroid, Vector2 impactDirection)
    {
        if (asteroid.Size == AsteroidSize.Small)
        {
            _debrisSystem.SpawnAsteroidDebris(_debris, asteroid, impactDirection);
            _asteroidSpawner.NotifyAsteroidDestroyed(asteroids, asteroid);
            asteroid.Active = false;
            _playRumble?.Invoke();
            return;
        }

        _asteroidSpawner.SplitAsteroid(asteroids, asteroid);
        _playRumble?.Invoke();
    }

    private static bool CirclesOverlap(Vector2 aPos, float aRadius, Vector2 bPos, float bRadius)
    {
        float combined = aRadius + bRadius;
        return Vector2.DistanceSquared(aPos, bPos) <= combined * combined;
    }
}
