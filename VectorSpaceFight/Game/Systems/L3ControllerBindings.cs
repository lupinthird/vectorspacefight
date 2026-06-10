using L3Controller.Input;

namespace VectorSpaceFight.Game.Systems;

/// <summary>
/// L3 controller slot assignment and axis/button polling for up to four players.
/// Controllers claim open slots on first activity; rotation locks to whichever
/// HID axis (Z spinner vs Rz paddle) moves from the initial baseline.
/// </summary>
public sealed class L3ControllerBindings
{
    public const int MaxSlots = 4;

    private enum RotationAxisKind
    {
        SpinnerZ,
        PaddleRz
    }

    private readonly ControllerManager _manager;
    private readonly string?[] _boundDeviceIds = new string?[MaxSlots];
    private readonly RotationAxisKind?[] _lockedRotationAxis = new RotationAxisKind?[MaxSlots];
    private readonly bool[] _rotationAxisLockChanged = new bool[MaxSlots];

    public L3ControllerBindings()
    {
        _manager = new ControllerManager(maxSlots: MaxSlots)
        {
            AutoClaimEnabled = false
        };

        if (OperatingSystem.IsWindows())
            _manager.Initialize();
    }

    public int ClaimedCount => _manager.ClaimedCount;

    public bool IsSlotConnected(int slot) =>
        slot >= 0 && slot < MaxSlots && _manager.GetSlot(slot) != null;

    public TrackedController? GetSlot(int slot) => _manager.GetSlot(slot);

    public bool SlotBindingChanged(int slot) =>
        slot >= 0
        && slot < MaxSlots
        && _manager.GetSlot(slot) is TrackedController tracked
        && tracked.Id != _boundDeviceIds[slot];

    public bool ConsumeRotationAxisLockChange(int slot)
    {
        if (slot < 0 || slot >= MaxSlots)
            return false;

        var changed = _rotationAxisLockChanged[slot];
        _rotationAxisLockChanged[slot] = false;
        return changed;
    }

    public void Update(float deltaSeconds)
    {
        if (!OperatingSystem.IsWindows())
            return;

        _manager.Update(deltaSeconds);
        ClaimControllersOnActivity();

        for (var slot = 0; slot < MaxSlots; slot++)
        {
            var tracked = _manager.GetSlot(slot);
            if (tracked?.Id != _boundDeviceIds[slot])
                _lockedRotationAxis[slot] = null;

            EnsureRotationAxisLocked(slot);
            _boundDeviceIds[slot] = tracked?.Id;
        }
    }

    public bool TryGetRotationAxisPosition(int slot, out float axis)
    {
        axis = 0f;
        if (slot < 0 || slot >= MaxSlots)
            return false;

        var tracked = _manager.GetSlot(slot);
        if (tracked == null || _lockedRotationAxis[slot] is not RotationAxisKind locked)
            return false;

        return ReadLockedRotationAxis(tracked, locked, out axis);
    }

    public bool WasButtonPressed(int slot, int buttonIndex)
    {
        var tracked = _manager.GetSlot(slot);
        if (tracked?.Previous == null || buttonIndex < 0)
            return false;

        var current = tracked.Current.Buttons;
        var previous = tracked.Previous.Buttons;
        if (buttonIndex >= current.Length)
            return false;

        return current[buttonIndex]
            && (buttonIndex >= previous.Length || !previous[buttonIndex]);
    }

    public bool IsButtonHeld(int slot, int buttonIndex)
    {
        var tracked = _manager.GetSlot(slot);
        return tracked != null
            && buttonIndex >= 0
            && buttonIndex < tracked.Current.Buttons.Length
            && tracked.Current.Buttons[buttonIndex];
    }

    public bool WasAnyButtonPressed(int buttonIndex)
    {
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            if (WasButtonPressed(slot, buttonIndex))
                return true;
        }

        return false;
    }

    public static DeviceProfile ResolveProfile(TrackedController tracked)
    {
        var fromName = L3ControllerIdentity.GetDeviceProfile(tracked.DisplayName);
        return fromName != DeviceProfile.Unknown ? fromName : tracked.Profile;
    }

    /// <summary>
    /// Maps normalized rotation axis (-1..1, one revolution) to ship heading radians.
    /// </summary>
    public static float MapRotationAxisToHeading(float normalizedAxis) =>
        normalizedAxis * MathF.PI;

    private void ClaimControllersOnActivity()
    {
        foreach (var controller in _manager.AllControllers)
        {
            if (controller.SlotIndex.HasValue || !controller.HasBaseline)
                continue;

            if (!HasClaimActivity(controller))
                continue;

            var slot = FindNextOpenSlot();
            if (slot < 0)
                break;

            if (!_manager.TryAssignToSlot(controller, slot))
                continue;

            controller.SourcesLocked = false;
            controller.SpinnerSource = SpinnerSourceKind.None;
            controller.PaddleSource = PaddleSourceKind.None;
        }
    }

    private int FindNextOpenSlot()
    {
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            if (_manager.GetSlot(slot) == null)
                return slot;
        }

        return -1;
    }

    private void EnsureRotationAxisLocked(int slot)
    {
        if (_lockedRotationAxis[slot].HasValue)
            return;

        var tracked = _manager.GetSlot(slot);
        if (tracked == null)
            return;

        if (!TryDetectDominantRotationAxis(tracked, out var kind))
            return;

        _lockedRotationAxis[slot] = kind;
        ApplyRotationSourceLock(tracked, kind);
        _rotationAxisLockChanged[slot] = true;
    }

    private static bool HasClaimActivity(TrackedController tracked)
    {
        if (UsesL3RotationAxes(tracked))
            return TryDetectDominantRotationAxis(tracked, out _);

        if (InputMapping.DetectButtonActivity(tracked.Current.Buttons, tracked.Previous?.Buttons))
            return true;

        if (ResolveProfile(tracked) != DeviceProfile.GenericGamepad)
            return false;

        var current = tracked.Current;
        var baseline = tracked.Baseline;

        if (InputMapping.DetectStickChangeFromBaseline(
                current.LeftStick, baseline.LeftStick, InputMapping.ActivityThreshold))
            return true;

        if (InputMapping.DetectStickChangeFromBaseline(
                current.RightStick, baseline.RightStick, InputMapping.ActivityThreshold))
            return true;

        return MathF.Abs(current.LeftTrigger - baseline.LeftTrigger) > InputMapping.ActivityThreshold
            || MathF.Abs(current.RightTrigger - baseline.RightTrigger) > InputMapping.ActivityThreshold;
    }

    private static bool UsesL3RotationAxes(TrackedController tracked) =>
        tracked.RawController != null
        || tracked.JoystickIndex.HasValue
        || L3ControllerIdentity.IsL3ControllerName(tracked.DisplayName);

    private static bool TryDetectDominantRotationAxis(TrackedController tracked, out RotationAxisKind kind)
    {
        var spinMotion = InputMapping.DetectAxisChangeFromBaseline(
            tracked.Current.RawZ,
            tracked.Baseline.RawZ,
            InputMapping.SpinnerDeltaThreshold);

        var potMotion = InputMapping.DetectAxisChangeFromBaseline(
            tracked.Current.RawRz,
            tracked.Baseline.RawRz,
            InputMapping.PaddleDeltaThreshold);

        if (potMotion && !spinMotion)
        {
            kind = RotationAxisKind.PaddleRz;
            return true;
        }

        if (spinMotion && !potMotion)
        {
            kind = RotationAxisKind.SpinnerZ;
            return true;
        }

        if (tracked.Previous != null)
        {
            var zDelta = MathF.Abs(L3ControllerIdentity.ComputeWrapAwareDelta(
                tracked.Current.RawZ ?? 0f,
                tracked.Previous.RawZ ?? 0f));

            var rzDelta = MathF.Abs(L3ControllerIdentity.ComputeWrapAwareDelta(
                tracked.Current.RawRz ?? 0f,
                tracked.Previous.RawRz ?? 0f));

            if (rzDelta >= InputMapping.PaddleDeltaThreshold && rzDelta > zDelta + 0.002f)
            {
                kind = RotationAxisKind.PaddleRz;
                return true;
            }

            if (zDelta >= InputMapping.SpinnerDeltaThreshold && zDelta > rzDelta + 0.002f)
            {
                kind = RotationAxisKind.SpinnerZ;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private static void ApplyRotationSourceLock(TrackedController tracked, RotationAxisKind kind)
    {
        tracked.SpinnerSource = SpinnerSourceKind.None;
        tracked.PaddleSource = PaddleSourceKind.None;

        switch (kind)
        {
            case RotationAxisKind.SpinnerZ when tracked.Current.RawZ.HasValue:
                tracked.SpinnerSource = tracked.RawController != null
                    ? SpinnerSourceKind.RawZ
                    : SpinnerSourceKind.JoystickZ;
                break;

            case RotationAxisKind.PaddleRz when tracked.Current.RawRz.HasValue:
                tracked.PaddleSource = tracked.RawController != null
                    ? PaddleSourceKind.RawRz
                    : PaddleSourceKind.JoystickRz;
                break;

            case RotationAxisKind.SpinnerZ when tracked.Current.HasLeftStick:
                tracked.SpinnerSource = SpinnerSourceKind.LeftStickX;
                break;

            case RotationAxisKind.PaddleRz when tracked.Current.HasLeftStick:
                tracked.PaddleSource = PaddleSourceKind.LeftStickY;
                break;
        }

        tracked.SourcesLocked = true;
    }

    private static bool ReadLockedRotationAxis(
        TrackedController tracked,
        RotationAxisKind kind,
        out float axis)
    {
        return kind switch
        {
            RotationAxisKind.PaddleRz => TryReadPaddleRotationAxis(tracked, out axis),
            RotationAxisKind.SpinnerZ => TryReadSpinnerRotationAxis(tracked, out axis),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static bool TryReadPaddleRotationAxis(TrackedController tracked, out float axis)
    {
        if (tracked.Current.RawRz is float rawRz)
        {
            axis = InputMapping.MapPaddleFromAxis(rawRz);
            return true;
        }

        if (tracked.PaddleSource != PaddleSourceKind.None)
        {
            axis = tracked.PaddlePosition;
            return true;
        }

        if (tracked.Current.HasLeftStick)
        {
            axis = InputMapping.MapPaddleFromAxis(tracked.Current.LeftStick.Y);
            return true;
        }

        axis = 0f;
        return false;
    }

    private static bool TryReadSpinnerRotationAxis(TrackedController tracked, out float axis)
    {
        if (tracked.Current.RawZ is float rawZ)
        {
            axis = rawZ;
            return true;
        }

        if (tracked.SpinnerSource == SpinnerSourceKind.LeftStickX)
        {
            axis = tracked.Current.LeftStick.X;
            return true;
        }

        axis = 0f;
        return false;
    }
}
