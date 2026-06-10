using L3Controller.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Systems;

public struct PlayerInputState
{
    public float Rotate;
    public bool UseAbsoluteHeading;
    public float AbsoluteHeading;
    public bool Thrust;
    public bool Shoot;
    public bool ShieldPressed;
    public bool StartPressed;
    public bool Connected;
}

public class InputSystem
{
    private readonly L3ControllerBindings _l3 = new();
    private readonly bool[] _previousShield = new bool[4];
    private readonly bool[] _previousStart = new bool[4];
    private readonly float[] _spinnerHeadingOffset = new float[4];
    private readonly bool[] _spinnerNeedsHeadingSync = new bool[4];
    private bool _previousKeyboardShield;
    private bool _previousKeyboardStart;

    public void Update(float deltaSeconds)
    {
        for (var slot = 0; slot < 4; slot++)
        {
            if (_l3.SlotBindingChanged(slot))
                _spinnerNeedsHeadingSync[slot] = true;
        }

        _l3.Update(deltaSeconds);

        for (var slot = 0; slot < 4; slot++)
        {
            if (_l3.ConsumeRotationAxisLockChange(slot))
                _spinnerNeedsHeadingSync[slot] = true;
        }
    }

    public void RequestSpinnerHeadingSync(int slot)
    {
        if (slot >= 0 && slot < _spinnerNeedsHeadingSync.Length)
            _spinnerNeedsHeadingSync[slot] = true;
    }

    public void RequestAllSpinnerHeadingSync()
    {
        for (var slot = 0; slot < _spinnerNeedsHeadingSync.Length; slot++)
            _spinnerNeedsHeadingSync[slot] = true;
    }

    public void SyncSpinnerHeadingIfNeeded(int slot, float shipRotation)
    {
        if (slot < 0 || slot >= _spinnerNeedsHeadingSync.Length || !_spinnerNeedsHeadingSync[slot])
            return;

        if (!_l3.TryGetRotationAxisPosition(slot, out var axis))
            return;

        _spinnerHeadingOffset[slot] =
            shipRotation - L3ControllerBindings.MapRotationAxisToHeading(axis);
        _spinnerNeedsHeadingSync[slot] = false;
    }

    public PlayerInputState[] ReadInput()
    {
        var states = new PlayerInputState[4];

        for (var i = 0; i < 4; i++)
        {
            if (_l3.IsSlotConnected(i))
            {
                states[i] = ReadL3Player(i);
                continue;
            }

            var pad = GamePad.GetState((PlayerIndex)i);
            states[i].Connected = pad.IsConnected;

            if (pad.IsConnected)
            {
                states[i].Rotate = ApplyDeadzone(pad.ThumbSticks.Left.X);
                states[i].Thrust = pad.Buttons.A == ButtonState.Pressed;
                states[i].Shoot = pad.Buttons.X == ButtonState.Pressed;
                states[i].ShieldPressed = pad.Buttons.B == ButtonState.Pressed && !_previousShield[i];
                states[i].StartPressed = pad.Buttons.Start == ButtonState.Pressed && !_previousStart[i];

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

    public bool WasStartPressed()
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Enter) && !_previousKeyboardStart)
            return true;

        if (_l3.WasAnyButtonPressed(0))
            return true;

        for (var i = 0; i < 4; i++)
        {
            var pad = GamePad.GetState((PlayerIndex)i);
            if (pad.IsConnected &&
                pad.Buttons.Start == ButtonState.Pressed &&
                !_previousStart[i])
            {
                return true;
            }
        }

        return false;
    }

    public void CaptureStartFrame()
    {
        _previousKeyboardStart = Keyboard.GetState().IsKeyDown(Keys.Enter);
        for (var i = 0; i < 4; i++)
            _previousStart[i] = GamePad.GetState((PlayerIndex)i).Buttons.Start == ButtonState.Pressed;
    }

    public int ClaimedControllerCount => _l3.ClaimedCount;

    private PlayerInputState ReadL3Player(int slot)
    {
        var tracked = _l3.GetSlot(slot);
        if (tracked == null)
            return default;

        var profile = L3ControllerBindings.ResolveProfile(tracked);
        if (profile == DeviceProfile.GenericGamepad)
            return ReadL3GenericGamepad(tracked, slot);

        var buttons = tracked.Current.Buttons;
        var state = new PlayerInputState
        {
            Connected = true,
            Thrust = buttons.Length > 1 && buttons[1],
            Shoot = buttons.Length > 0 && buttons[0],
            ShieldPressed = _l3.WasButtonPressed(slot, 2),
            StartPressed = _l3.WasButtonPressed(slot, 0)
        };

        if (_l3.TryGetRotationAxisPosition(slot, out var axis))
        {
            state.UseAbsoluteHeading = true;
            state.AbsoluteHeading =
                _spinnerHeadingOffset[slot] + L3ControllerBindings.MapRotationAxisToHeading(axis);
        }

        return state;
    }

    private PlayerInputState ReadL3GenericGamepad(TrackedController tracked, int slot)
    {
        var snapshot = tracked.Current;
        return new PlayerInputState
        {
            Connected = true,
            Rotate = ApplyDeadzone(snapshot.LeftStick.X),
            Thrust = snapshot.Buttons.Length > 0 && snapshot.Buttons[0],
            Shoot = snapshot.Buttons.Length > 2 && snapshot.Buttons[2],
            ShieldPressed = _l3.WasButtonPressed(slot, 1),
            StartPressed = _l3.WasButtonPressed(slot, 7)
        };
    }

    private void ApplyKeyboardFallback(PlayerInputState[] states)
    {
        var kb = Keyboard.GetState();
        var shieldDown = kb.IsKeyDown(Keys.C);
        var startDown = kb.IsKeyDown(Keys.Enter);

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
