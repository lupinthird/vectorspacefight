using Microsoft.Xna.Framework;

namespace VectorSpaceFight.Game.Rendering;

public static class VectorFont
{
    public const float CellWidth = VectorDigits.CellWidth;
    public const float CellHeight = VectorDigits.CellHeight;
    public const float LetterAdvance = VectorDigits.DigitAdvance;

    private const float L = 0.5f;
    private const float R = 3.5f;
    private const float T = 0.5f;
    private const float Bot = 7.5f;
    private const float UM = 3.8f;
    private const float LM = 4.2f;
    private const float Mid = 4f;
    private const float Cx = 2f;

    private static readonly Dictionary<char, (float X1, float Y1, float X2, float Y2)[]> Glyphs = BuildGlyphs();

    public static float MeasureText(string text, float scale)
    {
        float width = 0f;
        foreach (char c in text)
        {
            if (c == ' ')
                width += LetterAdvance * 0.6f * scale;
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
                cursor += LetterAdvance * 0.6f * scale;
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
        DrawPlayerName(batch, playerIndex, anchor, scale, color, hAlign);
    }

    public static float MeasurePlayerName(int playerIndex, float scale)
        => MeasureText($"PLAYER {playerIndex + 1}", scale);

    public static void DrawPlayerName(LineBatch batch, int playerIndex, Vector2 anchor, float scale, Color color,
        HorizontalAlign hAlign = HorizontalAlign.Left)
    {
        DrawText(batch, $"PLAYER {playerIndex + 1}", anchor, scale, color, hAlign);
    }

    private static Dictionary<char, (float, float, float, float)[]> BuildGlyphs()
    {
        return new Dictionary<char, (float, float, float, float)[]>
        {
            // Seven-segment "8" without the bottom bar — matches VectorDigits geometry.
            ['A'] = Seg(L, T, R, T, R, T, R, UM, R, LM, R, Bot, L, LM, L, Bot, L, T, L, UM, L, Mid, R, Mid),
            ['B'] = Seg(L, Bot, L, T, L, T, R, T, R, T, R, UM, 3f, Mid, L, Mid, 3f, Mid, R, LM, R, LM, R, Bot, R, Bot, L, Bot),
            ['C'] = Seg(R, T, L, T, L, T, L, Bot, L, Bot, R, Bot),
            ['E'] = Seg(L, T, L, Bot, L, T, R, T, L, Mid, 3f, Mid, L, Bot, R, Bot),
            ['F'] = Seg(L, T, L, Bot, L, T, R, T, L, Mid, 3f, Mid),
            ['G'] = Seg(R, T, L, T, L, T, L, Bot, L, Bot, R, Bot, R, Bot, R, LM, 2.5f, LM, R, LM),
            ['H'] = Seg(L, T, L, Bot, R, T, R, Bot, L, Mid, R, Mid),
            ['I'] = Seg(L, T, R, T, Cx, T, Cx, Bot, L, Bot, R, Bot),
            ['K'] = Seg(L, T, L, Bot, L, Mid, R, T, L, Mid, R, Bot),
            ['L'] = Seg(L, T, L, Bot, L, Bot, R, Bot),
            ['M'] = Seg(L, Bot, L, T, L, T, Cx, Mid, Cx, Mid, R, T, R, T, R, Bot),
            ['N'] = Seg(L, Bot, L, T, L, T, R, Bot, R, Bot, R, T),
            ['O'] = Seg(L, T, R, T, R, T, R, Bot, L, Bot, R, Bot, L, T, L, Bot),
            ['P'] = Seg(L, Bot, L, T, L, T, R, T, R, T, R, UM, 3f, Mid, L, Mid),
            ['R'] = Seg(L, Bot, L, T, L, T, R, T, R, T, R, UM, 3f, Mid, L, Mid, 2.2f, Mid, R, Bot),
            ['S'] = Seg(R, T, L, T, L, T, L, Mid, L, Mid, R, Mid, R, Mid, R, Bot, R, Bot, L, Bot),
            ['T'] = Seg(L, T, R, T, Cx, T, Cx, Bot),
            ['U'] = Seg(L, T, L, 6.8f, L, 6.8f, 1f, Bot, 1f, Bot, 3f, Bot, 3f, Bot, R, 6.8f, R, 6.8f, R, T),
            ['V'] = Seg(L, T, Cx, Bot, Cx, Bot, R, T),
            ['W'] = Seg(L, T, 1f, Bot, 1f, Bot, Cx, 3.2f, Cx, 3.2f, 3f, Bot, 3f, Bot, R, T),
            ['Y'] = Seg(L, T, Cx, Mid, R, T, Cx, Mid, Cx, Mid, Cx, Bot),
            ['.'] = Seg(Cx, 6.8f, Cx, Bot),
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
