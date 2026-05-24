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
    private readonly bool[] _previousStart = new bool[4];
    private bool _previousKeyboardStart;
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

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            _returnToMenu();
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            var pad = GamePad.GetState((PlayerIndex)i);
            bool startDown = pad.IsConnected && pad.Buttons.Start == ButtonState.Pressed;
            if (startDown && !_previousStart[i])
            {
                _rematch();
                return;
            }

            _previousStart[i] = startDown;
        }

        bool keyboardStart = Keyboard.GetState().IsKeyDown(Keys.Enter);
        if (keyboardStart && !_previousKeyboardStart)
            _rematch();
        _previousKeyboardStart = keyboardStart;
    }

    public void Draw(GameTime gameTime)
    {
        var device = _context.GraphicsDevice;
        device.SetRenderTarget(_context.SceneTarget);
        device.Clear(Color.Black);

        var winner = _ships.OrderByDescending(s => s.Kills).ThenBy(s => s.PlayerIndex).First();
        _context.Renderer.DrawResults(_ships, winner.PlayerIndex);

        _context.CRTEffect.Apply(_context.SpriteBatch, _context.SceneTarget, _elapsedTime);
    }
}
