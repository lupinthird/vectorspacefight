using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace VectorSpaceFight.Game.States;

public class MenuState : IGameState
{
    private readonly GameContext _context;
    private readonly Action _startMatch;
    private readonly bool[] _previousStart = new bool[4];
    private bool _previousKeyboardStart;
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

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            _context.Game.Exit();

        for (int i = 0; i < 4; i++)
        {
            var pad = GamePad.GetState((PlayerIndex)i);
            bool startDown = pad.IsConnected && pad.Buttons.Start == ButtonState.Pressed;
            if (startDown && !_previousStart[i])
            {
                _startMatch();
                return;
            }

            _previousStart[i] = startDown;
        }

        bool keyboardStart = Keyboard.GetState().IsKeyDown(Keys.Enter);
        if (keyboardStart && !_previousKeyboardStart)
            _startMatch();
        _previousKeyboardStart = keyboardStart;
    }

    public void Draw(GameTime gameTime)
    {
        var device = _context.GraphicsDevice;
        device.SetRenderTarget(_context.SceneTarget);
        device.Clear(Color.Black);

        _context.Renderer.DrawMenu(_elapsedTime);
        _context.CRTEffect.Apply(_context.SpriteBatch, _context.SceneTarget, _elapsedTime);
    }
}
