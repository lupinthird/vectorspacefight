using L3Controller.Input;

namespace VectorSpaceFight.Game.Systems;

/// <summary>
/// L3 controller claim-order slots and roster-color player assignment.
/// Claim slots fill top-to-bottom; roster binding waits for firmware serial color.
/// </summary>
public sealed class L3ControllerBindings
{
    public const int MaxSlots = PlayerRoster.Count;

    private enum RotationAxisKind
    {
        SpinnerZ,
        PaddleRz
    }

    private readonly ControllerManager _manager;
    private readonly TrackedController?[] _playerAssignments = new TrackedController?[MaxSlots];
    private readonly string?[] _boundDeviceIds = new string?[MaxSlots];
    private readonly RotationAxisKind?[] _lockedRotationAxis = new RotationAxisKind?[MaxSlots];
    private readonly bool[] _rotationAxisLockChanged = new bool[MaxSlots];
    private readonly float[] _slotActivityPulse = new float[MaxSlots];

    private string? _menuControllerDeviceId;

    public L3ControllerBindings()
    {
        _manager = new ControllerManager(maxSlots: MaxSlots);

        _manager.Initialize();
    }

    public int ClaimedCount => _manager.ClaimedCount;

    public bool IsPlayerConnected(int playerIndex) => GetPlayerController(playerIndex) != null;

    public TrackedController? GetClaimSlot(int claimIndex) => _manager.GetSlot(claimIndex);

    public TrackedController? GetPlayerController(int playerIndex) =>
        playerIndex >= 0 && playerIndex < _playerAssignments.Length
            ? _playerAssignments[playerIndex]
            : null;

    public bool IsSlotAssigned(int claimIndex) => GetClaimSlot(claimIndex) != null;

    public bool IsSlotColorIdentified(int claimIndex)
    {
        var tracked = GetClaimSlot(claimIndex);
        return tracked != null && RosterColor.TryGetPlayerIndexFromSerial(tracked, out _);
    }

    public int? GetAssignedPlayerIndex(int claimIndex)
    {
        var tracked = GetClaimSlot(claimIndex);
        if (tracked == null)
            return null;

        return RosterColor.TryGetPlayerIndexFromSerial(tracked, out var playerIndex)
            ? playerIndex
            : null;
    }

    public string GetSlotDisplayLabel(int claimIndex)
    {
        var tracked = GetClaimSlot(claimIndex);
        if (tracked == null)
            return string.Empty;

        if (tracked.TryGetDeviceInfo(out var info))
            return info.DisplayName;

        if (tracked.IsL3Device
            && !string.IsNullOrWhiteSpace(tracked.ClaimLabel)
            && !tracked.ClaimLabel.Equals("Controller", StringComparison.OrdinalIgnoreCase))
        {
            return tracked.ClaimLabel;
        }

        if (L3ControllerIdentity.IsL3SerialNumber(tracked.DeviceSerialNumber))
            return L3ControllerIdentity.FormatDisplayNameFromSerial(tracked.DeviceSerialNumber);

        return string.Empty;
    }

    public bool IsMenuControllerSlot(int claimIndex) =>
        claimIndex == 0 && IsSlotAssigned(0);

    public float GetSlotActivityPulse(int claimIndex) =>
        claimIndex >= 0 && claimIndex < _slotActivityPulse.Length
            ? _slotActivityPulse[claimIndex]
            : 0f;

    public bool CanStartGame() =>
        ClaimedCount >= 1 && _menuControllerDeviceId != null;

    public bool PlayerBindingChanged(int playerIndex) =>
        playerIndex >= 0
        && playerIndex < MaxSlots
        && GetPlayerController(playerIndex) is TrackedController tracked
        && tracked.Id != _boundDeviceIds[playerIndex];

    public bool ConsumeRotationAxisLockChange(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= MaxSlots)
            return false;

        var changed = _rotationAxisLockChanged[playerIndex];
        _rotationAxisLockChanged[playerIndex] = false;
        return changed;
    }

    public void ResetMenuSetup()
    {
        _manager.ResetClaims();
        _menuControllerDeviceId = null;
        Array.Clear(_playerAssignments, 0, _playerAssignments.Length);
        Array.Clear(_boundDeviceIds, 0, _boundDeviceIds.Length);
        Array.Clear(_lockedRotationAxis, 0, _lockedRotationAxis.Length);
        Array.Clear(_rotationAxisLockChanged, 0, _rotationAxisLockChanged.Length);
        Array.Clear(_slotActivityPulse, 0, _slotActivityPulse.Length);
    }

    public void ApplyClaimsToSession(GameSession session) =>
        session.ApplyClaimedSlots(IsPlayerConnected);

    public void Update(float deltaSeconds)
    {
        _manager.Update(deltaSeconds);
        RefreshPlayerAssignments();
        TrackMenuController();
        DecayActivityPulse(deltaSeconds);

        for (var playerIndex = 0; playerIndex < MaxSlots; playerIndex++)
        {
            var tracked = GetPlayerController(playerIndex);
            if (tracked?.Id != _boundDeviceIds[playerIndex])
                _lockedRotationAxis[playerIndex] = null;

            EnsureRotationAxisLocked(playerIndex);
            _boundDeviceIds[playerIndex] = tracked?.Id;
        }

        for (var claimIndex = 0; claimIndex < MaxSlots; claimIndex++)
        {
            var claimTracked = GetClaimSlot(claimIndex);
            if (claimTracked == null)
            {
                _slotActivityPulse[claimIndex] = 0f;
                continue;
            }

            UpdateClaimSlotActivity(claimIndex, claimTracked);
        }
    }

    public bool TryGetRotationAxisPosition(int playerIndex, out float axis)
    {
        axis = 0f;
        if (playerIndex < 0 || playerIndex >= MaxSlots)
            return false;

        var tracked = GetPlayerController(playerIndex);
        if (tracked == null || _lockedRotationAxis[playerIndex] is not RotationAxisKind locked)
            return false;

        return ReadLockedRotationAxis(tracked, locked, out axis);
    }

    public bool WasButtonPressed(int playerIndex, int buttonIndex)
    {
        var tracked = GetPlayerController(playerIndex);
        if (tracked?.Previous == null || buttonIndex < 0)
            return false;

        var current = tracked.Current.Buttons;
        var previous = tracked.Previous.Buttons;
        if (buttonIndex >= current.Length)
            return false;

        return current[buttonIndex]
            && (buttonIndex >= previous.Length || !previous[buttonIndex]);
    }

    public bool IsButtonHeld(int playerIndex, int buttonIndex)
    {
        var tracked = GetPlayerController(playerIndex);
        return tracked != null
            && buttonIndex >= 0
            && buttonIndex < tracked.Current.Buttons.Length
            && tracked.Current.Buttons[buttonIndex];
    }

    public bool WasMenuConfirmPressed()
    {
        if (!CanStartGame() || _menuControllerDeviceId == null)
            return false;

        if (_manager.FindById(_menuControllerDeviceId) is not TrackedController menuController)
            return false;

        return WasFirePressed(menuController);
    }

    public bool WasAnyHumanButtonPressed(int buttonIndex)
    {
        for (var playerIndex = 0; playerIndex < MaxSlots; playerIndex++)
        {
            if (IsPlayerConnected(playerIndex) && WasButtonPressed(playerIndex, buttonIndex))
                return true;
        }

        return false;
    }

    public static DeviceProfile ResolveProfile(TrackedController tracked)
    {
        var fromName = L3ControllerIdentity.GetDeviceProfile(tracked.DisplayName);
        return fromName != DeviceProfile.Unknown ? fromName : tracked.Profile;
    }

    public static float MapRotationAxisToHeading(float normalizedAxis) =>
        normalizedAxis * MathF.PI;

    private void RefreshPlayerAssignments()
    {
        Array.Clear(_playerAssignments, 0, _playerAssignments.Length);
        var fallbackByClaimOrder = new List<(int ClaimIndex, TrackedController Controller)>();

        for (var claimIndex = 0; claimIndex < MaxSlots; claimIndex++)
        {
            var controller = GetClaimSlot(claimIndex);
            if (controller == null)
                continue;

            if (RosterColor.TryGetPlayerIndexFromSerial(controller, out var colorSlot))
            {
                if (_playerAssignments[colorSlot] == null)
                    _playerAssignments[colorSlot] = controller;
                continue;
            }

            if (IsAwaitingSerialIdentity(controller))
                continue;

            fallbackByClaimOrder.Add((claimIndex, controller));
        }

        var freeSlots = new Queue<int>();
        for (var playerIndex = 0; playerIndex < MaxSlots; playerIndex++)
        {
            if (_playerAssignments[playerIndex] == null)
                freeSlots.Enqueue(playerIndex);
        }

        foreach (var (claimIndex, controller) in fallbackByClaimOrder.OrderBy(entry => entry.ClaimIndex))
        {
            if (freeSlots.Count == 0)
                break;

            _playerAssignments[freeSlots.Dequeue()] = controller;
        }
    }

    private static bool IsAwaitingSerialIdentity(TrackedController controller) =>
        controller.Profile != DeviceProfile.GenericGamepad
        && string.IsNullOrWhiteSpace(controller.DeviceSerialNumber);

    private void TrackMenuController()
    {
        if (_menuControllerDeviceId != null)
            return;

        var firstClaimed = _manager.GetSlot(0);
        if (firstClaimed != null)
            _menuControllerDeviceId = firstClaimed.Id;
    }

    private void EnsureRotationAxisLocked(int playerIndex)
    {
        if (_lockedRotationAxis[playerIndex].HasValue)
            return;

        var tracked = GetPlayerController(playerIndex);
        if (tracked == null)
            return;

        if (!TryDetectDominantRotationAxis(tracked, out var kind))
            return;

        _lockedRotationAxis[playerIndex] = kind;
        ApplyRotationSourceLock(tracked, kind);
        _rotationAxisLockChanged[playerIndex] = true;
    }

    private static bool WasFirePressed(TrackedController controller)
    {
        var current = controller.Current;
        var previous = controller.Previous;
        if (previous == null || current.Buttons.Length == 0)
            return false;

        var fireNow = current.Buttons[0];
        var firePrev = previous.Buttons.Length > 0 && previous.Buttons[0];
        return fireNow && !firePrev;
    }

    private void UpdateClaimSlotActivity(int claimIndex, TrackedController tracked)
    {
        if (tracked.Previous?.RawZ is float prevZ && tracked.Current.RawZ is float currentZ)
        {
            var spinMotion = MathF.Abs(L3ControllerIdentity.ComputeWrapAwareDelta(currentZ, prevZ));
            if (spinMotion >= 0.002f)
            {
                _slotActivityPulse[claimIndex] = MathF.Min(1f, _slotActivityPulse[claimIndex] + spinMotion * 5f);
                return;
            }
        }

        if (tracked.Previous?.RawRz is float prevRz && tracked.Current.RawRz is float currentRz)
        {
            var potMotion = MathF.Abs(L3ControllerIdentity.ComputeWrapAwareDelta(currentRz, prevRz));
            if (potMotion >= 0.018f)
                _slotActivityPulse[claimIndex] = MathF.Min(1f, _slotActivityPulse[claimIndex] + potMotion * 5f);
        }
    }

    private void DecayActivityPulse(float deltaSeconds)
    {
        var decay = MathF.Pow(0.02f, deltaSeconds);
        for (var i = 0; i < MaxSlots; i++)
        {
            if (!IsSlotAssigned(i))
            {
                _slotActivityPulse[i] = 0f;
                continue;
            }

            _slotActivityPulse[i] *= decay;
            if (_slotActivityPulse[i] < 0.02f)
                _slotActivityPulse[i] = 0f;
        }
    }

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
                tracked.SpinnerSource = tracked.PrefersRawGameControllerSource
                    ? SpinnerSourceKind.RawZ
                    : SpinnerSourceKind.JoystickZ;
                break;

            case RotationAxisKind.PaddleRz when tracked.Current.RawRz.HasValue:
                tracked.PaddleSource = tracked.PrefersRawGameControllerSource
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
