using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Systems;

public static class PlayerPalette
{
    public static readonly Color MenuSlotGray = new(150, 150, 158);

    public static readonly Color[] PlayerColors =
    [
        new(220, 70, 70),    // RED
        new(80, 140, 230),   // BLUE
        new(80, 190, 110),   // GREEN
        new(230, 200, 70),   // YELLOW
    ];

    public static Color GetPlayerColor(int playerIndex) =>
        playerIndex >= 0 && playerIndex < PlayerColors.Length
            ? PlayerColors[playerIndex]
            : MenuSlotGray;
}
