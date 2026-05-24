using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Entities;

public class LineDebris
{
    public Vector2 Position;
    public Vector2 Direction;
    public float HalfLength;
    public Vector2 Velocity;
    public float AngularVelocity;
    public float Rotation;
    public float Lifetime;
    public Color Color;
    public bool Active;

    public void Spawn(Vector2 start, Vector2 end, Vector2 velocity, Vector2 impactDirection, int fragmentIndex, int fragmentCount, Color color)
    {
        Position = (start + end) * 0.5f;
        Direction = end - start;
        if (Direction.LengthSquared() > 0.001f)
            Direction.Normalize();
        else
            Direction = Vector2.UnitX;

        HalfLength = Vector2.Distance(start, end) * 0.5f;
        Rotation = MathF.Atan2(Direction.Y, Direction.X);

        var impact = impactDirection.LengthSquared() > 0.001f
            ? Vector2.Normalize(impactDirection)
            : new Vector2(0f, -1f);

        float fan = MathF.PI / 3f;
        float t = fragmentCount <= 1 ? 0f : fragmentIndex / (float)(fragmentCount - 1) - 0.5f;
        float angle = MathF.Atan2(impact.Y, impact.X) + t * fan;
        var scatter = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        float speed = 90f + fragmentIndex * 18f;

        Velocity = velocity + scatter * speed;
        AngularVelocity = (fragmentIndex % 2 == 0 ? 1f : -1f) * (4f + fragmentIndex);
        Lifetime = 0.85f;
        Color = color;
        Active = true;
    }

    public void Update(float dt)
    {
        if (!Active)
            return;

        Lifetime -= dt;
        if (Lifetime <= 0f)
        {
            Active = false;
            return;
        }

        Position += Velocity * dt;
        Rotation += AngularVelocity * dt;
        Velocity *= 0.98f;
    }

    public (Vector2 Start, Vector2 End) GetSegment()
    {
        var offset = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation)) * HalfLength;
        return (Position - offset, Position + offset);
    }
}
