using Microsoft.Xna.Framework;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Entities;

public class Asteroid
{
    public Vector2 Position;
    public Vector2 Velocity;
    public AsteroidSize Size;
    public Vector2[] Vertices = Array.Empty<Vector2>();
    public float Rotation;
    public float RotationSpeed;
    public bool Active;
    public int LineageId;

    public float Radius => GameConstants.GetAsteroidRadius(Size);

    public void Initialize(AsteroidSize size, Vector2 position, Vector2 velocity, Random random)
    {
        Size = size;
        Position = position;
        Velocity = velocity;
        Rotation = (float)(random.NextDouble() * Math.PI * 2);
        RotationSpeed = (float)(random.NextDouble() * 2 - 1) * (size == AsteroidSize.Large ? 0.8f : 1.5f);
        Vertices = GenerateVertices(size, random);
        Active = true;
        LineageId = 0;
    }

    public (Vector2 Start, Vector2 End)[] GetWorldEdges()
    {
        var world = GetWorldVertices();
        var edges = new (Vector2, Vector2)[world.Length];
        for (int i = 0; i < world.Length; i++)
            edges[i] = (world[i], world[(i + 1) % world.Length]);
        return edges;
    }

    public Vector2[] GetWorldVertices()
    {
        var cos = MathF.Cos(Rotation);
        var sin = MathF.Sin(Rotation);
        var world = new Vector2[Vertices.Length];
        for (int i = 0; i < Vertices.Length; i++)
        {
            var v = Vertices[i];
            world[i] = Position + new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        return world;
    }

    private static Vector2[] GenerateVertices(AsteroidSize size, Random random)
    {
        int count = size switch
        {
            AsteroidSize.Large => random.Next(6, 9),
            AsteroidSize.Medium => random.Next(5, 7),
            _ => random.Next(4, 6)
        };

        float baseRadius = GameConstants.GetAsteroidRadius(size);
        var vertices = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * MathF.Tau;
            float radius = baseRadius * (0.75f + (float)random.NextDouble() * 0.35f);
            vertices[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        return vertices;
    }
}
