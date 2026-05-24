using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.Systems;

public class AsteroidSpawner
{
    private readonly Random _random = new();
    private float _trickleTimer;

    public void Reset(List<Asteroid> asteroids)
    {
        asteroids.Clear();
        _trickleTimer = GameConstants.AsteroidSpawnInterval;

        for (int i = 0; i < GameConstants.InitialLargeAsteroids; i++)
            SpawnAsteroid(asteroids, AsteroidSize.Large, GetSafeSpawnPosition());
    }

    public void Update(List<Asteroid> asteroids, float dt)
    {
        _trickleTimer -= dt;
        if (_trickleTimer <= 0f)
        {
            _trickleTimer = GameConstants.AsteroidSpawnInterval;
            SpawnAsteroid(asteroids, AsteroidSize.Small, GetSafeSpawnPosition());
        }
    }

    public void SplitAsteroid(List<Asteroid> asteroids, Asteroid asteroid)
    {
        if (asteroid.Size == AsteroidSize.Small)
        {
            asteroid.Active = false;
            return;
        }

        var nextSize = asteroid.Size == AsteroidSize.Large ? AsteroidSize.Medium : AsteroidSize.Small;
        asteroid.Active = false;

        for (int i = 0; i < 2; i++)
        {
            var velocity = RandomVelocity(nextSize);
            SpawnAsteroid(asteroids, nextSize, asteroid.Position, velocity);
        }
    }

    private void SpawnAsteroid(List<Asteroid> asteroids, AsteroidSize size, Vector2 position, Vector2? velocity = null)
    {
        var asteroid = GetInactiveAsteroid(asteroids);
        asteroid.Initialize(size, position, velocity ?? RandomVelocity(size), _random);
    }

    private Asteroid GetInactiveAsteroid(List<Asteroid> asteroids)
    {
        foreach (var asteroid in asteroids)
        {
            if (!asteroid.Active)
                return asteroid;
        }

        var created = new Asteroid();
        asteroids.Add(created);
        return created;
    }

    private Vector2 GetSafeSpawnPosition()
    {
        for (int attempt = 0; attempt < 32; attempt++)
        {
            var position = new Vector2(
                (float)_random.NextDouble() * GameConstants.WorldWidth,
                (float)_random.NextDouble() * GameConstants.WorldHeight);

            if (IsAwayFromSpawns(position))
                return position;
        }

        return new Vector2(GameConstants.WorldWidth * 0.5f, GameConstants.WorldHeight * 0.5f);
    }

    private static bool IsAwayFromSpawns(Vector2 position)
    {
        for (int i = 0; i < 4; i++)
        {
            if (Vector2.DistanceSquared(position, GameConstants.GetSpawnPosition(i)) < 120f * 120f)
                return false;
        }

        return true;
    }

    private Vector2 RandomVelocity(AsteroidSize size)
    {
        float speed = size switch
        {
            AsteroidSize.Large => 40f,
            AsteroidSize.Medium => 70f,
            _ => 110f
        };

        float angle = (float)_random.NextDouble() * MathF.Tau;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
    }
}
