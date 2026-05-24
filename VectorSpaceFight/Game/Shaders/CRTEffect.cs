using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Shaders;

public sealed class CRTEffect : IDisposable
{
    private readonly Effect _effect;
    private readonly VertexBuffer _vertexBuffer;

    public float BloomIntensity { get; } = GameConstants.BloomDefaultIntensity;

    public CRTEffect(Effect effect, GraphicsDevice device)
    {
        _effect = effect;
        _vertexBuffer = CreateFullscreenQuad(device);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, float time)
    {
        _effect.Parameters["TextureSize"]?.SetValue(new Vector2(source.Width, source.Height));
        _effect.Parameters["Time"]?.SetValue(time);
        _effect.Parameters["BloomIntensity"]?.SetValue(BloomIntensity);
        _effect.Parameters["SceneTexture"]?.SetValue(source);

        var device = spriteBatch.GraphicsDevice;
        device.SetRenderTarget(null);
        device.Clear(Color.Black);
        device.SetVertexBuffer(_vertexBuffer);
        device.RasterizerState = RasterizerState.CullNone;
        device.DepthStencilState = DepthStencilState.None;
        device.BlendState = BlendState.Opaque;
        device.SamplerStates[0] = SamplerState.LinearClamp;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
        }
    }

    private static VertexBuffer CreateFullscreenQuad(GraphicsDevice device)
    {
        var vertices = new[]
        {
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1))
        };

        var buffer = new VertexBuffer(device, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
        buffer.SetData(vertices);
        return buffer;
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
    }
}
