using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Systems;

public struct PlayerInputState
{
    public float Rotate;
    public bool Thrust;
    public bool Shoot;
    public bool ShieldPressed;
    public bool StartPressed;
    public bool Connected;
}

public class InputSystem
{
    private readonly bool[] _previousShield = new bool[4];
    private readonly bool[] _previousStart = new bool[4];
    private bool _previousKeyboardShield;
    private bool _previousKeyboardStart;

    public PlayerInputState[] ReadInput()
    {
        var states = new PlayerInputState[4];

        for (int i = 0; i < 4; i++)
        {
            var pad = GamePad.GetState((PlayerIndex)i);
            states[i].Connected = pad.IsConnected;

            if (pad.IsConnected)
            {
                states[i].Rotate = ApplyDeadzone(pad.ThumbSticks.Left.X);
                states[i].Thrust = pad.Buttons.A == ButtonState.Pressed;
                states[i].Shoot = pad.Buttons.X == ButtonState.Pressed;
                states[i].ShieldPressed = pad.Buttons.B == ButtonState.Pressed &&
                                          !_previousShield[i];
                states[i].StartPressed = pad.Buttons.Start == ButtonState.Pressed &&
                                         !_previousStart[i];

                _previousShield[i] = pad.Buttons.B == ButtonState.Pressed;
                _previousStart[i] = pad.Buttons.Start == ButtonState.Pressed;
            }
            else
            {
                _previousShield[i] = false;
                _previousStart[i] = false;
            }
        }

        if (!states[0].Connected)
            ApplyKeyboardFallback(states);

        return states;
    }

    private void ApplyKeyboardFallback(PlayerInputState[] states)
    {
        var kb = Keyboard.GetState();
        bool shieldDown = kb.IsKeyDown(Keys.C);
        bool startDown = kb.IsKeyDown(Keys.Enter);

        states[0].Connected = true;
        states[0].Rotate = (kb.IsKeyDown(Keys.Right) ? 1f : 0f) -
                           (kb.IsKeyDown(Keys.Left) ? 1f : 0f);
        states[0].Thrust = kb.IsKeyDown(Keys.Z);
        states[0].Shoot = kb.IsKeyDown(Keys.X);
        states[0].ShieldPressed = shieldDown && !_previousKeyboardShield;
        states[0].StartPressed = startDown && !_previousKeyboardStart;

        _previousKeyboardShield = shieldDown;
        _previousKeyboardStart = startDown;
    }

    private static float ApplyDeadzone(float value)
    {
        const float deadzone = GameConstants.StickDeadzone;
        if (MathF.Abs(value) < deadzone)
            return 0f;

        return value;
    }
}
