using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Rendering;

public sealed class LineBatch : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly DynamicVertexBuffer _vertexBuffer;
    private readonly VertexPositionColor[] _vertices;
    private int _vertexCount;

    private const int MaxVertices = 16384;

    public LineBatch(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            VertexColorEnabled = true,
            TextureEnabled = false,
            LightingEnabled = false
        };
        _vertices = new VertexPositionColor[MaxVertices];
        _vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColor), MaxVertices, BufferUsage.WriteOnly);
    }

    public void Begin()
    {
        _vertexCount = 0;
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        AddVertex(start, color);
        AddVertex(end, color);
    }

    public void DrawTriangle(Vector2 center, float rotation, float size, Color color)
    {
        var points = GetShipPoints(center, rotation, size);
        DrawLine(points[0], points[1], color);
        DrawLine(points[1], points[2], color);
        DrawLine(points[2], points[0], color);
    }

    public void DrawCircle(Vector2 center, float radius, Color color, int segments = 24)
    {
        float step = MathF.Tau / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;
            var p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
            var p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
            DrawLine(p0, p1, color);
        }
    }

    public void DrawPolygon(Vector2 center, Vector2[] localVertices, float rotation, Color color)
    {
        if (localVertices.Length < 2)
            return;

        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        var world = new Vector2[localVertices.Length];
        for (int i = 0; i < localVertices.Length; i++)
        {
            var v = localVertices[i];
            world[i] = center + new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }

        for (int i = 0; i < world.Length; i++)
            DrawLine(world[i], world[(i + 1) % world.Length], color);
    }

    public void Flush(Matrix viewProjection)
    {
        if (_vertexCount < 2)
            return;

        _effect.View = viewProjection;
        _effect.Projection = Matrix.Identity;
        _effect.World = Matrix.Identity;

        _vertexBuffer.SetData(_vertices, 0, _vertexCount, SetDataOptions.Discard);

        _device.SetVertexBuffer(_vertexBuffer);
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawPrimitives(PrimitiveType.LineList, 0, _vertexCount / 2);
        }
    }

    public static Vector2[] GetShipPoints(Vector2 center, float rotation, float size)
    {
        var facing = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));
        var right = new Vector2(facing.Y, -facing.X);
        var nose = center + facing * size;
        var tailLeft = center - facing * size * 0.6f + right * size * 0.55f;
        var tailRight = center - facing * size * 0.6f - right * size * 0.55f;
        return new[] { nose, tailLeft, tailRight };
    }

    private void AddVertex(Vector2 position, Color color)
    {
        if (_vertexCount >= MaxVertices)
            return;

        _vertices[_vertexCount++] = new VertexPositionColor(new Vector3(position, 0f), BoostColor(color));
    }

    private static Color BoostColor(Color color)
    {
        var boosted = color * GameConstants.VectorLineIntensity;
        boosted.A = color.A;
        return boosted;
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _effect.Dispose();
    }
}
