using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.Systems;

public static class WrapSystem
{
    public static void Wrap(ref Vector2 position)
    {
        position.X = Mod(position.X, GameConstants.WorldWidth);
        position.Y = Mod(position.Y, GameConstants.WorldHeight);
    }

    public static void WrapShip(Ship ship)
    {
        Wrap(ref ship.Position);
    }

    public static void WrapBullets(List<Bullet> bullets)
    {
        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            Wrap(ref bullet.Position);
        }
    }

    public static void WrapAsteroids(List<Asteroid> asteroids)
    {
        foreach (var asteroid in asteroids)
        {
            if (!asteroid.Active)
                continue;

            Wrap(ref asteroid.Position);
        }
    }

    public static IEnumerable<Vector2> GetWrapOffsets(Vector2 position, float margin)
    {
        yield return Vector2.Zero;

        bool nearLeft = position.X < margin;
        bool nearRight = position.X > GameConstants.WorldWidth - margin;
        bool nearTop = position.Y < margin;
        bool nearBottom = position.Y > GameConstants.WorldHeight - margin;

        if (nearLeft)
            yield return new Vector2(GameConstants.WorldWidth, 0);
        if (nearRight)
            yield return new Vector2(-GameConstants.WorldWidth, 0);
        if (nearTop)
            yield return new Vector2(0, GameConstants.WorldHeight);
        if (nearBottom)
            yield return new Vector2(0, -GameConstants.WorldHeight);

        if (nearLeft && nearTop)
            yield return new Vector2(GameConstants.WorldWidth, GameConstants.WorldHeight);
        if (nearRight && nearTop)
            yield return new Vector2(-GameConstants.WorldWidth, GameConstants.WorldHeight);
        if (nearLeft && nearBottom)
            yield return new Vector2(GameConstants.WorldWidth, -GameConstants.WorldHeight);
        if (nearRight && nearBottom)
            yield return new Vector2(-GameConstants.WorldWidth, -GameConstants.WorldHeight);
    }

    private static float Mod(float value, float size)
    {
        value %= size;
        if (value < 0f)
            value += size;
        return value;
    }
}
