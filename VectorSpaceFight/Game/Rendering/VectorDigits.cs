using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Rendering;

public enum HorizontalAlign
{
    Left,
    Center,
    Right
}

public enum VerticalAlign
{
    Top,
    Center,
    Bottom
}

public static class VectorDigits
{
    public const float CellWidth = 4f;
    public const float CellHeight = 8f;
    public const float DigitAdvance = 5f;

    private static readonly bool[][] Segments =
    {
        new[] { true, true, true, true, true, true, false },       // 0
        new[] { false, true, true, false, false, false, false },   // 1
        new[] { true, true, false, true, true, false, true },      // 2
        new[] { true, true, true, true, false, false, true },      // 3
        new[] { false, true, true, false, false, true, true },     // 4
        new[] { true, false, true, true, false, true, true },      // 5
        new[] { true, false, true, true, true, true, true },       // 6
        new[] { true, true, true, false, false, false, false },    // 7
        new[] { true, true, true, true, true, true, true },        // 8
        new[] { true, true, true, true, false, true, true },       // 9
    };

    public static float MeasureWidth(int number, float scale, int minDigits = 1)
    {
        int digitCount = Math.Max(minDigits, Math.Max(0, number).ToString().Length);
        return digitCount * DigitAdvance * scale;
    }

    public static void DrawNumber(LineBatch batch, int number, Vector2 anchor, float scale, Color color,
        HorizontalAlign hAlign = HorizontalAlign.Left, VerticalAlign vAlign = VerticalAlign.Top, int minDigits = 1)
    {
        string text = Math.Max(0, number).ToString().PadLeft(minDigits, '0');
        float width = text.Length * DigitAdvance * scale;
        float height = CellHeight * scale;

        float x = hAlign switch
        {
            HorizontalAlign.Center => anchor.X - width * 0.5f,
            HorizontalAlign.Right => anchor.X - width,
            _ => anchor.X
        };

        float y = vAlign switch
        {
            VerticalAlign.Center => anchor.Y - height * 0.5f,
            VerticalAlign.Bottom => anchor.Y - height,
            _ => anchor.Y
        };

        float cursor = x;
        foreach (char c in text)
        {
            if (c >= '0' && c <= '9')
                DrawDigitPublic(batch, c - '0', new Vector2(cursor, y), scale, color);
            cursor += DigitAdvance * scale;
        }
    }

    public static void DrawColon(LineBatch batch, Vector2 anchor, float scale, Color color)
    {
        float dot = scale * 0.7f;
        float centerY = anchor.Y + CellHeight * scale * 0.5f;
        batch.DrawLine(new Vector2(anchor.X, centerY - scale * 1.2f), new Vector2(anchor.X, centerY - scale * 1.2f + dot), color);
        batch.DrawLine(new Vector2(anchor.X, centerY + scale * 0.5f), new Vector2(anchor.X, centerY + scale * 0.5f + dot), color);
    }

    public static void DrawTime(LineBatch batch, float seconds, Vector2 anchor, float scale, Color color)
    {
        int total = Math.Max(0, (int)seconds);
        int minutes = total / 60;
        int secs = total % 60;

        float minuteWidth = MeasureWidth(minutes, scale);
        float secondWidth = MeasureWidth(secs, scale, minDigits: 2);
        float colonWidth = 1.5f * scale;
        float totalWidth = minuteWidth + colonWidth + secondWidth;
        float x = anchor.X - totalWidth * 0.5f;
        float y = anchor.Y;

        DrawNumber(batch, minutes, new Vector2(x, y), scale, color);
        DrawColon(batch, new Vector2(x + minuteWidth + colonWidth * 0.5f, y), scale, color);
        DrawNumber(batch, secs, new Vector2(x + minuteWidth + colonWidth, y), scale, color, minDigits: 2);
    }

    public static void DrawDigitPublic(LineBatch batch, int digit, Vector2 origin, float scale, Color color)
        => DrawDigit(batch, digit, origin, scale, color);

    private static void DrawDigit(LineBatch batch, int digit, Vector2 origin, float scale, Color color)
    {
        var segments = Segments[digit];

        if (segments[0]) batch.DrawLine(origin + new Vector2(0.5f, 0.5f) * scale, origin + new Vector2(3.5f, 0.5f) * scale, color);
        if (segments[1]) batch.DrawLine(origin + new Vector2(3.5f, 0.5f) * scale, origin + new Vector2(3.5f, 3.8f) * scale, color);
        if (segments[2]) batch.DrawLine(origin + new Vector2(3.5f, 4.2f) * scale, origin + new Vector2(3.5f, 7.5f) * scale, color);
        if (segments[3]) batch.DrawLine(origin + new Vector2(0.5f, 7.5f) * scale, origin + new Vector2(3.5f, 7.5f) * scale, color);
        if (segments[4]) batch.DrawLine(origin + new Vector2(0.5f, 4.2f) * scale, origin + new Vector2(0.5f, 7.5f) * scale, color);
        if (segments[5]) batch.DrawLine(origin + new Vector2(0.5f, 0.5f) * scale, origin + new Vector2(0.5f, 3.8f) * scale, color);
        if (segments[6]) batch.DrawLine(origin + new Vector2(0.5f, 4f) * scale, origin + new Vector2(3.5f, 4f) * scale, color);
    }
}
