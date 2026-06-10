using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VectorSpaceFight.Game.Entities;

namespace VectorSpaceFight.Game.States;

public class ResultsState : IGameState
{
    private readonly GameContext _context;
    private readonly Ship[] _ships;
    private readonly Action _rematch;
    private readonly Action _returnToMenu;
    private float _elapsedTime;

    public ResultsState(GameContext context, Ship[] ships, Action rematch, Action returnToMenu)
    {
        _context = context;
        _ships = ships;
        _rematch = rematch;
        _returnToMenu = returnToMenu;
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
            _rematch();
    }

    public void Draw(GameTime gameTime)
    {
        var device = _context.GraphicsDevice;
        device.SetRenderTarget(_context.SceneTarget);
        device.Clear(Color.Black);

        var winner = _ships.OrderByDescending(s => s.Kills).ThenBy(s => s.PlayerIndex).First();
        _context.Renderer.DrawResults(_ships, winner.PlayerIndex);

        _context.PostProcess.Apply(_context.SpriteBatch, _context.SceneTarget, _elapsedTime, _context.RenderSettings);
        _context.Renderer.DrawShaderTuningHud(_context.RenderSettings);
    }
}
