using L3Controller.Input;

namespace VectorSpaceFight.Game.Systems;

/// <summary>Maps L3 firmware serial color to VectorSpaceFight roster slots.</summary>
internal static class RosterColor
{
    public static bool TryGetPlayerIndexFromSerial(TrackedController controller, out int playerIndex)
    {
        playerIndex = -1;
        if (!controller.IsL3Device || controller.ControllerColor == L3ControllerColor.Unknown)
            return false;

        return L3ControllerIdentity.TryGetPlayerSlotIndex(controller.ControllerColor, out playerIndex);
    }
}
