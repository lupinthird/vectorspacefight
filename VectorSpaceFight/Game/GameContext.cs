using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Audio;
using VectorSpaceFight.Game.Rendering;
using VectorSpaceFight.Game.Shaders;
using VectorSpaceFight.Game.Systems;

namespace VectorSpaceFight.Game;

public class GameContext
{
    public required GraphicsDevice GraphicsDevice { get; init; }
    public required ContentManager Content { get; init; }
    public required SpriteBatch SpriteBatch { get; init; }
    public required LineBatch LineBatch { get; init; }
    public required GameRenderer Renderer { get; init; }
    public required PostProcessEffect PostProcess { get; init; }
    public required RenderSettings RenderSettings { get; init; }
    public required RenderTarget2D SceneTarget { get; init; }
    public required ProceduralAudioSystem Audio { get; init; }
    public required VectorSpaceFightGame Game { get; init; }
    public required InputSystem Input { get; init; }
    public GameSession Session { get; } = new();
}
