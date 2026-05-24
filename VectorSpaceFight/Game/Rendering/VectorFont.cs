using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Rendering;

public static class VectorFont
{
    public const float CellWidth = 5f;
    public const float CellHeight = VectorDigits.CellHeight;
    public const float LetterAdvance = VectorDigits.DigitAdvance;

    private static readonly Dictionary<char, (float X1, float Y1, float X2, float Y2)[]> Glyphs = BuildGlyphs();

    public static float MeasureText(string text, float scale)
    {
        float width = 0f;
        foreach (char c in text)
        {
            if (c == ' ')
                width += 3f * scale;
            else
                width += LetterAdvance * scale;
        }

        return width;
    }

    public static void DrawText(LineBatch batch, string text, Vector2 anchor, float scale, Color color,
        HorizontalAlign hAlign = HorizontalAlign.Left, VerticalAlign vAlign = VerticalAlign.Top)
    {
        float width = MeasureText(text, scale);
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
            if (c == ' ')
            {
                cursor += 3f * scale;
                continue;
            }

            if (c >= '0' && c <= '9')
            {
                VectorDigits.DrawDigitPublic(batch, c - '0', new Vector2(cursor, y), scale, color);
                cursor += LetterAdvance * scale;
                continue;
            }

            if (Glyphs.TryGetValue(char.ToUpper(c), out var strokes))
            {
                foreach (var stroke in strokes)
                {
                    batch.DrawLine(
                        new Vector2(cursor + stroke.X1 * scale, y + stroke.Y1 * scale),
                        new Vector2(cursor + stroke.X2 * scale, y + stroke.Y2 * scale),
                        color);
                }
            }

            cursor += LetterAdvance * scale;
        }
    }

    public static void DrawPlayerLabel(LineBatch batch, int playerIndex, Vector2 anchor, float scale, Color color,
        HorizontalAlign hAlign = HorizontalAlign.Left)
    {
        float width = MeasureText("P", scale) + VectorDigits.MeasureWidth(playerIndex + 1, scale);
        float x = hAlign switch
        {
            HorizontalAlign.Center => anchor.X - width * 0.5f,
            HorizontalAlign.Right => anchor.X - width,
            _ => anchor.X
        };

        DrawText(batch, "P", new Vector2(x, anchor.Y), scale, color);
        VectorDigits.DrawNumber(batch, playerIndex + 1, new Vector2(x + MeasureText("P", scale), anchor.Y), scale, color);
    }

    private static Dictionary<char, (float, float, float, float)[]> BuildGlyphs()
    {
        return new Dictionary<char, (float, float, float, float)[]>
        {
            ['A'] = Seg(0, 8, 2.5f, 0, 2.5f, 0, 5, 0, 8, 5, 8, 1.5f, 4, 3.5f, 4),
            ['C'] = Seg(5, 0, 1, 0, 0, 2, 0, 6, 1, 8, 5, 8),
            ['E'] = Seg(0, 0, 0, 8, 0, 0, 5, 0, 0, 4, 4, 4, 0, 8, 5, 8),
            ['F'] = Seg(0, 0, 0, 8, 0, 0, 5, 0, 0, 4, 4, 4),
            ['G'] = Seg(5, 0, 1, 0, 0, 2, 0, 6, 1, 8, 5, 8, 5, 8, 5, 4.5f, 3.5f, 4.5f),
            ['H'] = Seg(0, 0, 0, 8, 5, 0, 5, 8, 0, 4, 5, 4),
            ['I'] = Seg(1, 0, 4, 0, 2.5f, 0, 2.5f, 8, 1, 8, 4, 8),
            ['K'] = Seg(0, 0, 0, 8, 0, 4, 5, 0, 0, 4, 5, 8),
            ['L'] = Seg(0, 0, 0, 8, 0, 8, 5, 8),
            ['M'] = Seg(0, 8, 0, 0, 0, 0, 2.5f, 4, 2.5f, 4, 5, 0, 5, 0, 5, 8),
            ['N'] = Seg(0, 8, 0, 0, 0, 0, 5, 8, 5, 8, 5, 0),
            ['O'] = Seg(1, 0, 4, 0, 0, 2, 0, 6, 1, 8, 4, 8, 5, 6, 5, 2, 4, 0, 1, 0),
            ['P'] = Seg(0, 8, 0, 0, 0, 0, 4, 0, 4, 0, 5, 2, 5, 3.5f, 4, 4, 0, 4),
            ['R'] = Seg(0, 8, 0, 0, 0, 0, 4, 0, 4, 0, 5, 2, 5, 3.5f, 4, 4, 0, 4, 5, 8),
            ['S'] = Seg(4.5f, 1, 1.5f, 0, 0, 1.5f, 0, 3.2f, 2.2f, 4, 4.8f, 4.8f, 5, 6.2f, 3.2f, 8, 0.8f, 7, 0, 5.8f),
            ['T'] = Seg(0, 0, 5, 0, 2.5f, 0, 2.5f, 8),
            ['U'] = Seg(0, 0, 0, 6, 1, 8, 4, 8, 5, 6, 5, 0),
            ['V'] = Seg(0, 0, 2.5f, 8, 2.5f, 8, 5, 0),
            ['W'] = Seg(0, 0, 1.5f, 8, 1.5f, 8, 2.5f, 3, 2.5f, 3, 3.5f, 8, 3.5f, 8, 5, 0),
            ['.'] = Seg(2.2f, 7.2f, 2.8f, 7.8f),
        };
    }

    private static (float, float, float, float)[] Seg(params float[] v)
    {
        var strokes = new (float, float, float, float)[v.Length / 4];
        for (int i = 0; i < strokes.Length; i++)
            strokes[i] = (v[i * 4], v[i * 4 + 1], v[i * 4 + 2], v[i * 4 + 3]);
        return strokes;
    }
}
