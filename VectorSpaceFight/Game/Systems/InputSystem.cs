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
        for (var playerIndex = 0; playerIndex < PlayerRoster.Count; playerIndex++)
        {
            if (_l3.PlayerBindingChanged(playerIndex))
                _spinnerNeedsHeadingSync[playerIndex] = true;
        }

        _l3.Update(deltaSeconds);

        for (var playerIndex = 0; playerIndex < PlayerRoster.Count; playerIndex++)
        {
            if (_l3.ConsumeRotationAxisLockChange(playerIndex))
                _spinnerNeedsHeadingSync[playerIndex] = true;
        }
    }

    public void ResetMenuSetup() => _l3.ResetMenuSetup();

    public bool CanStartGame() => _l3.CanStartGame();

    public bool WasMenuConfirmPressed() => _l3.WasMenuConfirmPressed();

    public bool TryConsumeMenuExitHold(float deltaSeconds, bool enabled) =>
        _l3.TryConsumeMenuExitHold(deltaSeconds, enabled);

    public void ApplyClaimsToSession(GameSession session) => _l3.ApplyClaimsToSession(session);

    public bool IsSlotAssigned(int claimIndex) => _l3.IsSlotAssigned(claimIndex);

    public bool IsSlotColorIdentified(int claimIndex) => _l3.IsSlotColorIdentified(claimIndex);

    public int? GetAssignedPlayerIndex(int claimIndex) => _l3.GetAssignedPlayerIndex(claimIndex);

    public string GetSlotDisplayLabel(int claimIndex) => _l3.GetSlotDisplayLabel(claimIndex);

    public bool IsMenuControllerSlot(int claimIndex) => _l3.IsMenuControllerSlot(claimIndex);

    public float GetSlotActivityPulse(int claimIndex) => _l3.GetSlotActivityPulse(claimIndex);

    public int ClaimedControllerCount => _l3.ClaimedCount;

    public Color GetSlotTextColor(int claimIndex)
    {
        var tracked = _l3.GetClaimSlot(claimIndex);
        if (tracked == null)
            return PlayerPalette.MenuSlotGray;

        if (RosterColor.TryGetPlayerIndexFromSerial(tracked, out var paletteIndex))
            return PlayerPalette.GetPlayerColor(paletteIndex);

        return PlayerPalette.MenuSlotGray;
    }

    public void RequestSpinnerHeadingSync(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < _spinnerNeedsHeadingSync.Length)
            _spinnerNeedsHeadingSync[playerIndex] = true;
    }

    public void RequestAllSpinnerHeadingSync()
    {
        for (var playerIndex = 0; playerIndex < _spinnerNeedsHeadingSync.Length; playerIndex++)
            _spinnerNeedsHeadingSync[playerIndex] = true;
    }

    public void SyncSpinnerHeadingIfNeeded(int playerIndex, float shipRotation)
    {
        if (playerIndex < 0 || playerIndex >= _spinnerNeedsHeadingSync.Length || !_spinnerNeedsHeadingSync[playerIndex])
            return;

        if (!_l3.TryGetRotationAxisPosition(playerIndex, out var axis))
            return;

        _spinnerHeadingOffset[playerIndex] =
            shipRotation - L3ControllerBindings.MapRotationAxisToHeading(axis);
        _spinnerNeedsHeadingSync[playerIndex] = false;
    }

    public PlayerInputState[] ReadInput()
    {
        var states = new PlayerInputState[4];

        for (var i = 0; i < 4; i++)
        {
            if (_l3.IsPlayerConnected(i))
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

        if (_l3.WasAnyHumanButtonPressed(0))
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

    private PlayerInputState ReadL3Player(int playerIndex)
    {
        var tracked = _l3.GetPlayerController(playerIndex);
        if (tracked == null)
            return default;

        var profile = L3ControllerBindings.ResolveProfile(tracked);
        if (profile == DeviceProfile.GenericGamepad)
            return ReadL3GenericGamepad(tracked, playerIndex);

        var buttons = tracked.Current.Buttons;
        var state = new PlayerInputState
        {
            Connected = true,
            Thrust = buttons.Length > 1 && buttons[1],
            Shoot = buttons.Length > 0 && buttons[0],
            ShieldPressed = _l3.WasButtonPressed(playerIndex, 2),
            StartPressed = _l3.WasButtonPressed(playerIndex, 0)
        };

        if (_l3.TryGetRotationAxisPosition(playerIndex, out var axis))
        {
            state.UseAbsoluteHeading = true;
            state.AbsoluteHeading =
                _spinnerHeadingOffset[playerIndex] + L3ControllerBindings.MapRotationAxisToHeading(axis);
        }

        return state;
    }

    private PlayerInputState ReadL3GenericGamepad(TrackedController tracked, int playerIndex)
    {
        var snapshot = tracked.Current;
        return new PlayerInputState
        {
            Connected = true,
            Rotate = ApplyDeadzone(snapshot.LeftStick.X),
            Thrust = snapshot.Buttons.Length > 0 && snapshot.Buttons[0],
            Shoot = snapshot.Buttons.Length > 2 && snapshot.Buttons[2],
            ShieldPressed = _l3.WasButtonPressed(playerIndex, 1),
            StartPressed = _l3.WasButtonPressed(playerIndex, 7)
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
