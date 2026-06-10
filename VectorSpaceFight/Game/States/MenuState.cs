using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace VectorSpaceFight.Game.States;

public class MenuState : IGameState
{
    private readonly GameContext _context;
    private readonly Action _startMatch;
    private float _elapsedTime;

    public MenuState(GameContext context, Action startMatch)
    {
        _context = context;
        _startMatch = startMatch;
    }

    public void Enter()
    {
        _elapsedTime = 0f;
    }

    public void Exit()
    {
    }

    public void Update(GameTime gameTime)
    {
        _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_context.Input.WasStartPressed())
            _startMatch();
    }

    public void Draw(GameTime gameTime)
    {
        var device = _context.GraphicsDevice;
        device.SetRenderTarget(_context.SceneTarget);
        device.Clear(Color.Black);

        _context.Renderer.DrawMenu(_elapsedTime);
        _context.PostProcess.Apply(_context.SpriteBatch, _context.SceneTarget, _elapsedTime, _context.RenderSettings);
        _context.Renderer.DrawShaderTuningHud(_context.RenderSettings);
    }
}
