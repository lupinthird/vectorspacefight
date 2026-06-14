using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Rendering;

namespace VectorSpaceFight.Game.Shaders;

public sealed class PostProcessEffect : IDisposable
{
    private readonly Effect? _effect;
    private readonly VertexBuffer? _vertexBuffer;

    public PostProcessEffect(Effect? effect, GraphicsDevice device)
    {
        _effect = effect;
        _vertexBuffer = effect != null ? CreateFullscreenQuad(device) : null;
    }

    public void Present(SpriteBatch spriteBatch, RenderTarget2D source, float time, RenderSettings settings)
    {
        var device = spriteBatch.GraphicsDevice;
        device.SetRenderTarget(null);
        device.Clear(Color.Black);

        if (!settings.PostProcessEnabled || _effect == null || _vertexBuffer == null)
        {
            Blit(spriteBatch, source);
            return;
        }

        Apply(spriteBatch, source, time, settings);
    }

    private void Apply(SpriteBatch spriteBatch, RenderTarget2D source, float time, RenderSettings settings)
    {
        _effect!.Parameters["TextureSize"]?.SetValue(new Vector2(source.Width, source.Height));
        _effect.Parameters["Time"]?.SetValue(time);
        _effect.Parameters["BloomIntensity"]?.SetValue(settings.BloomIntensity);
        _effect.Parameters["NeonGlowIntensity"]?.SetValue(settings.NeonGlowIntensity);
        _effect.Parameters["BloomEnabled"]?.SetValue(RenderSettings.AsFlag(settings.Bloom));
        _effect.Parameters["NeonGlowEnabled"]?.SetValue(RenderSettings.AsFlag(settings.NeonGlow));
        _effect.Parameters["NeonCoreEnabled"]?.SetValue(RenderSettings.AsFlag(settings.NeonCore));
        _effect.Parameters["NeonTubeExpandEnabled"]?.SetValue(RenderSettings.AsFlag(settings.NeonTubeExpand));
        _effect.Parameters["ScanlinesEnabled"]?.SetValue(RenderSettings.AsFlag(settings.Scanlines));
        _effect.Parameters["PhosphorEnabled"]?.SetValue(RenderSettings.AsFlag(settings.PhosphorMask));
        _effect.Parameters["VignetteEnabled"]?.SetValue(RenderSettings.AsFlag(settings.Vignette));
        _effect.Parameters["NoiseEnabled"]?.SetValue(RenderSettings.AsFlag(settings.FilmNoise));
        _effect.Parameters["SceneTexture"]?.SetValue(source);

        var device = spriteBatch.GraphicsDevice;
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

    private static void Blit(SpriteBatch spriteBatch, Texture2D source)
    {
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
        spriteBatch.Draw(source, spriteBatch.GraphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
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
        _vertexBuffer?.Dispose();
    }
}
