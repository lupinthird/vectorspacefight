using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Entities;

public class Bullet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public int OwnerIndex;
    public float Lifetime;
    public bool Active = true;

    public void Spawn(Vector2 position, Vector2 direction, int ownerIndex)
    {
        Position = position;
        Velocity = direction * GameConstants.BulletSpeed;
        OwnerIndex = ownerIndex;
        Lifetime = GameConstants.BulletLifetime;
        Active = true;
    }
}
