using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Entities;
using VectorSpaceFight.Game.Systems;

namespace VectorSpaceFight.Game.Rendering;

public class GameRenderer
{
    private readonly LineBatch _lineBatch;

    public GameRenderer(LineBatch lineBatch)
    {
        _lineBatch = lineBatch;
    }

    public void DrawWorld(Ship[] ships, List<Bullet> bullets, List<Asteroid> asteroids, List<LineDebris> debris, float time)
    {
        _lineBatch.Begin();

        foreach (var asteroid in asteroids)
        {
            if (!asteroid.Active)
                continue;

            foreach (var offset in WrapSystem.GetWrapOffsets(asteroid.Position, GameConstants.WrapGhostMargin))
                DrawAsteroid(asteroid, offset);
        }

        foreach (var piece in debris)
        {
            if (!piece.Active)
                continue;

            foreach (var offset in WrapSystem.GetWrapOffsets(piece.Position, GameConstants.WrapGhostMargin))
                DrawDebris(piece, offset);
        }

        foreach (var bullet in bullets)
        {
            if (!bullet.Active)
                continue;

            var color = GameConstants.PlayerColors[bullet.OwnerIndex];
            foreach (var offset in WrapSystem.GetWrapOffsets(bullet.Position, GameConstants.WrapGhostMargin))
            {
                _lineBatch.DrawLine(bullet.Position + offset - new Vector2(2, 0), bullet.Position + offset + new Vector2(2, 0), color * 0.35f);
                _lineBatch.DrawLine(bullet.Position + offset, bullet.Position + offset + bullet.Velocity * 0.02f, color);
            }
        }

        foreach (var ship in ships)
        {
            if (!ship.IsAlive)
                continue;

            foreach (var offset in WrapSystem.GetWrapOffsets(ship.Position, GameConstants.WrapGhostMargin))
                DrawShip(ship, offset, time);
        }

        _lineBatch.Flush(CreateViewProjection());
    }

    public void DrawBloomTuner(float bloomIntensity)
    {
        _lineBatch.Begin();

        const float scale = 1.5f;
        var anchor = new Vector2(16f, GameConstants.WorldHeight - 16f);
        var label = $"BLOOM {bloomIntensity:F2}  [ -  ] +  \\ RESET";
        VectorFont.DrawText(_lineBatch, label, anchor, scale, Color.White * 0.45f, vAlign: VerticalAlign.Bottom);

        _lineBatch.Flush(CreateViewProjection());
    }

    public void DrawMatchHud(Ship[] ships, float matchTimer)
    {
        _lineBatch.Begin();

        VectorDigits.DrawTime(_lineBatch, matchTimer, new Vector2(GameConstants.WorldWidth * 0.5f, 24f),
            GameConstants.HudTimerScale, Color.White * 0.55f);

        for (int i = 0; i < ships.Length; i++)
        {
            var anchor = GameConstants.GetQuadrantScoreAnchor(i);
            var hAlign = i is 1 or 3 ? HorizontalAlign.Right : HorizontalAlign.Left;
            var vAlign = i is 2 or 3 ? VerticalAlign.Bottom : VerticalAlign.Top;
            VectorDigits.DrawNumber(_lineBatch, ships[i].Kills, anchor, GameConstants.HudScoreScale, ships[i].Color, hAlign, vAlign);
        }

        _lineBatch.Flush(CreateViewProjection());
    }

    public void DrawResults(Ship[] ships, int winnerIndex)
    {
        _lineBatch.Begin();

        const float titleScale = 3f;
        const float rowScale = 2.2f;
        const float hintScale = 1.8f;

        VectorFont.DrawText(_lineBatch, "MATCH OVER", new Vector2(GameConstants.WorldWidth * 0.5f, 80f),
            titleScale, Color.White, HorizontalAlign.Center);

        var ranked = ships
            .OrderByDescending(s => s.Kills)
            .ThenBy(s => s.PlayerIndex)
            .ToArray();

        const float labelGap = 0.8f;
        const float scoreGap = 1.6f;

        for (int rank = 0; rank < ranked.Length; rank++)
        {
            var ship = ranked[rank];
            float y = 200f + rank * 56f;
            var rowColor = ship.Color * (ship.PlayerIndex == winnerIndex ? 1f : 0.7f);
            rowColor.A = 255;

            float rowWidth = VectorDigits.MeasureWidth(rank + 1, rowScale) +
                             VectorFont.MeasureText(".", rowScale) +
                             labelGap * rowScale +
                             VectorFont.MeasureText("P", rowScale) + VectorDigits.MeasureWidth(ship.PlayerIndex + 1, rowScale) +
                             scoreGap * rowScale +
                             VectorFont.MeasureText("KILLS", rowScale) +
                             scoreGap * rowScale +
                             VectorDigits.MeasureWidth(ship.Kills, rowScale * 1.2f);
            float x = (GameConstants.WorldWidth - rowWidth) * 0.5f;

            VectorDigits.DrawNumber(_lineBatch, rank + 1, new Vector2(x, y), rowScale, rowColor);
            x += VectorDigits.MeasureWidth(rank + 1, rowScale);
            VectorFont.DrawText(_lineBatch, ".", new Vector2(x, y), rowScale, rowColor);
            x += VectorFont.MeasureText(".", rowScale) + labelGap * rowScale;
            VectorFont.DrawPlayerLabel(_lineBatch, ship.PlayerIndex, new Vector2(x, y), rowScale, rowColor);
            x += VectorFont.MeasureText("P", rowScale) + VectorDigits.MeasureWidth(ship.PlayerIndex + 1, rowScale) + scoreGap * rowScale;
            VectorFont.DrawText(_lineBatch, "KILLS", new Vector2(x, y), rowScale, rowColor * 0.75f);
            x += VectorFont.MeasureText("KILLS", rowScale) + scoreGap * rowScale;
            VectorDigits.DrawNumber(_lineBatch, ship.Kills, new Vector2(x, y), rowScale * 1.2f, rowColor);
        }

        var winner = ships[winnerIndex];
        float winnerWidth = VectorFont.MeasureText("WINNER", rowScale) + rowScale +
                            VectorFont.MeasureText("P", rowScale) + VectorDigits.MeasureWidth(winner.PlayerIndex + 1, rowScale * 1.3f);
        float winnerX = (GameConstants.WorldWidth - winnerWidth) * 0.5f;
        VectorFont.DrawText(_lineBatch, "WINNER", new Vector2(winnerX, 460f), rowScale, winner.Color);
        VectorFont.DrawPlayerLabel(_lineBatch, winner.PlayerIndex,
            new Vector2(winnerX + VectorFont.MeasureText("WINNER", rowScale) + rowScale, 460f), rowScale * 1.3f, winner.Color);

        VectorFont.DrawText(_lineBatch, "START TO REMATCH", new Vector2(GameConstants.WorldWidth * 0.5f, 540f),
            hintScale, Color.Yellow, HorizontalAlign.Center);
        VectorFont.DrawText(_lineBatch, "ESC FOR MENU", new Vector2(GameConstants.WorldWidth * 0.5f, 580f),
            hintScale, Color.Gray, HorizontalAlign.Center);

        _lineBatch.Flush(CreateViewProjection());
    }

    public void DrawMenu(float time)
    {
        _lineBatch.Begin();

        float pulse = 0.5f + MathF.Sin(time * 3f) * 0.5f;
        var center = new Vector2(GameConstants.WorldWidth * 0.5f, GameConstants.WorldHeight * 0.5f);

        _lineBatch.DrawLine(center + new Vector2(-18, 0), center + new Vector2(18, 0), Color.White * pulse);
        _lineBatch.DrawLine(center + new Vector2(0, -18), center + new Vector2(0, 18), Color.White * pulse);

        for (int i = 0; i < 4; i++)
        {
            var anchor = GameConstants.GetQuadrantScoreAnchor(i);
            var hAlign = i is 1 or 3 ? HorizontalAlign.Right : HorizontalAlign.Left;
            var vAlign = i is 2 or 3 ? VerticalAlign.Bottom : VerticalAlign.Top;
            _lineBatch.DrawTriangle(GetCornerTriangleCenter(anchor, hAlign, vAlign), GetCornerRotation(i), 12f, GameConstants.PlayerColors[i] * 0.8f);
        }

        _lineBatch.Flush(CreateViewProjection());
    }

    private void DrawShip(Ship ship, Vector2 offset, float time)
    {
        var position = ship.Position + offset;

        if (ship.IsThrusting)
            DrawThrustFlame(position, ship, time);

        var glow = ship.Color * 0.42f;
        glow.A = 255;

        _lineBatch.DrawTriangle(position, ship.Rotation, 16f, glow);
        _lineBatch.DrawTriangle(position, ship.Rotation, 14f, ship.Color);

        if (ship.ShieldActive)
            _lineBatch.DrawCircle(position, GameConstants.ShipRadius + 10f, ship.Color);
    }

    private void DrawThrustFlame(Vector2 position, Ship ship, float time)
    {
        var facing = ship.Facing;
        var right = new Vector2(facing.Y, -facing.X);
        float seed = ship.PlayerIndex * 2.17f;

        float flickerA = 0.5f + 0.5f * MathF.Sin(time * 28f + seed);
        float flickerB = 0.5f + 0.5f * MathF.Sin(time * 37f + seed * 1.6f);
        float length = 12f + flickerA * 11f;
        float halfWidth = 5f + flickerB * 3.5f;

        var exhaust = position - facing * 8.4f;
        float jag1 = MathF.Sin(time * 44f + seed) * 2.8f;
        float jag2 = MathF.Sin(time * 57f + seed + 1.3f) * 2.2f;
        float tipWobble = MathF.Sin(time * 63f + seed * 0.7f) * 1.6f;

        var outer = new[]
        {
            exhaust - right * halfWidth,
            exhaust + right * halfWidth,
            exhaust - facing * (length * 0.48f) + right * (halfWidth * 0.42f + jag1),
            exhaust - facing * length + right * tipWobble,
            exhaust - facing * (length * 0.48f) - right * (halfWidth * 0.42f - jag1),
        };

        var outerColor = Color.Lerp(ship.Color, new Color(255, 128, 32), 0.5f) * (0.5f + flickerA * 0.4f);
        outerColor.A = 255;
        DrawPolygonOutline(outer, outerColor);

        float coreLength = length * (0.55f + flickerB * 0.15f);
        float coreWidth = halfWidth * 0.45f;
        var core = new[]
        {
            exhaust - right * coreWidth,
            exhaust + right * coreWidth,
            exhaust - facing * coreLength + right * jag2 * 0.35f,
            exhaust - facing * (coreLength * 0.92f) - right * jag1 * 0.25f,
        };

        var coreColor = Color.Lerp(new Color(255, 220, 96), Color.White, flickerA * 0.35f) * (0.65f + flickerB * 0.35f);
        coreColor.A = 255;
        DrawPolygonOutline(core, coreColor);
    }

    private void DrawPolygonOutline(ReadOnlySpan<Vector2> points, Color color)
    {
        for (int i = 0; i < points.Length; i++)
            _lineBatch.DrawLine(points[i], points[(i + 1) % points.Length], color);
    }

    public void DrawLeaderHighlights(Ship[] ships, float time)
    {
        _lineBatch.Begin();

        foreach (var ship in ships)
        {
            if (!ship.IsAlive || ship.LeaderHighlightTimer <= 0f)
                continue;

            foreach (var offset in WrapSystem.GetWrapOffsets(ship.Position, GameConstants.WrapGhostMargin))
                DrawLeaderCrosshair(ship.Position + offset, time);
        }

        _lineBatch.Flush(CreateViewProjection());
    }

    private void DrawLeaderCrosshair(Vector2 position, float time)
    {
        float pulse = 0.65f + MathF.Sin(time * 8f) * 0.35f;
        const float arm = 30f;
        const float gap = 8f;
        var color = Color.Yellow * pulse;
        color.A = 255;

        _lineBatch.DrawLine(position + new Vector2(-arm, 0), position + new Vector2(-gap, 0), color);
        _lineBatch.DrawLine(position + new Vector2(gap, 0), position + new Vector2(arm, 0), color);
        _lineBatch.DrawLine(position + new Vector2(0, -arm), position + new Vector2(0, -gap), color);
        _lineBatch.DrawLine(position + new Vector2(0, gap), position + new Vector2(0, arm), color);
    }

    private void DrawDebris(LineDebris piece, Vector2 offset)
    {
        var segment = piece.GetSegment();
        var fade = Math.Clamp(piece.Lifetime / 0.85f, 0f, 1f);
        var color = piece.Color * fade;
        color.A = 255;
        _lineBatch.DrawLine(segment.Start + offset, segment.End + offset, color);
    }

    private void DrawAsteroid(Asteroid asteroid, Vector2 offset)
    {
        var color = new Color(230, 230, 230);
        var dim = color * 0.42f;
        dim.A = 255;
        _lineBatch.DrawPolygon(asteroid.Position + offset, asteroid.Vertices, asteroid.Rotation, dim);
        _lineBatch.DrawPolygon(asteroid.Position + offset, asteroid.Vertices, asteroid.Rotation, color);
    }

    private static Vector2 GetCornerTriangleCenter(Vector2 anchor, HorizontalAlign hAlign, VerticalAlign vAlign)
    {
        float inset = 36f;
        float x = hAlign == HorizontalAlign.Right ? anchor.X - inset : anchor.X + inset;
        float y = vAlign == VerticalAlign.Bottom ? anchor.Y - inset : anchor.Y + inset;
        return new Vector2(x, y);
    }

    private static float GetCornerRotation(int playerIndex) => playerIndex switch
    {
        0 => -MathF.PI * 0.75f,
        1 => -MathF.PI * 0.25f,
        2 => MathF.PI * 0.75f,
        _ => MathF.PI * 0.25f
    };

    private static Matrix CreateViewProjection()
    {
        return Matrix.CreateOrthographicOffCenter(0, GameConstants.WorldWidth, GameConstants.WorldHeight, 0, 0, 1);
    }
}
