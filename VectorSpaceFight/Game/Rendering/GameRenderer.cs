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

    public void DrawMatchHud(Ship[] ships, float matchTimer)
    {
        _lineBatch.Begin();

        VectorDigits.DrawTime(_lineBatch, matchTimer, new Vector2(GameConstants.WorldWidth * 0.5f, 24f),
            GameConstants.HudTimerScale, Color.White * 0.5f);

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

        const float headerScale = 1.8f;
        const float columnGap = 72f;
        const float headerRowY = 168f;
        const float rowStartY = 210f;
        const float rowSpacing = 56f;
        var headerColor = Color.White * 0.55f;
        headerColor.A = 255;

        float playerColumnWidth = VectorFont.MeasureText("PLAYER", headerScale);
        float killsColumnWidth = VectorFont.MeasureText("KILLS", headerScale);

        foreach (var ship in ranked)
        {
            playerColumnWidth = MathF.Max(playerColumnWidth, VectorFont.MeasurePlayerName(ship.PlayerIndex, rowScale));
            killsColumnWidth = MathF.Max(killsColumnWidth, VectorDigits.MeasureWidth(ship.Kills, rowScale));
        }

        float tableWidth = playerColumnWidth + columnGap + killsColumnWidth;
        float playerColumnX = (GameConstants.WorldWidth - tableWidth) * 0.5f;
        float killsColumnX = playerColumnX + playerColumnWidth + columnGap;
        float killsColumnRight = killsColumnX + killsColumnWidth;

        VectorFont.DrawText(_lineBatch, "PLAYER", new Vector2(playerColumnX, headerRowY), headerScale, headerColor);
        VectorFont.DrawText(_lineBatch, "KILLS", new Vector2(killsColumnRight, headerRowY), headerScale, headerColor,
            HorizontalAlign.Right);

        for (int rank = 0; rank < ranked.Length; rank++)
        {
            var ship = ranked[rank];
            float y = rowStartY + rank * rowSpacing;
            var rowColor = ship.Color * (ship.PlayerIndex == winnerIndex ? 1f : 0.7f);
            rowColor.A = 255;

            VectorFont.DrawPlayerName(_lineBatch, ship.PlayerIndex, new Vector2(playerColumnX, y), rowScale, rowColor);
            VectorDigits.DrawNumber(_lineBatch, ship.Kills, new Vector2(killsColumnRight, y), rowScale, rowColor,
                HorizontalAlign.Right);
        }

        var winner = ships[winnerIndex];
        float winnerWidth = VectorFont.MeasureText("WINNER", rowScale) + rowScale +
                            VectorFont.MeasurePlayerName(winner.PlayerIndex, rowScale * 1.3f);
        float winnerX = (GameConstants.WorldWidth - winnerWidth) * 0.5f;
        VectorFont.DrawText(_lineBatch, "WINNER", new Vector2(winnerX, 460f), rowScale, winner.Color);
        VectorFont.DrawPlayerName(_lineBatch, winner.PlayerIndex,
            new Vector2(winnerX + VectorFont.MeasureText("WINNER", rowScale) + rowScale, 460f), rowScale * 1.3f, winner.Color);

        VectorFont.DrawText(_lineBatch, "START TO REMATCH", new Vector2(GameConstants.WorldWidth * 0.5f, 540f),
            hintScale, Color.Yellow, HorizontalAlign.Center);
        VectorFont.DrawText(_lineBatch, "ESC FOR MENU", new Vector2(GameConstants.WorldWidth * 0.5f, 580f),
            hintScale, Color.Gray, HorizontalAlign.Center);

        _lineBatch.Flush(CreateViewProjection());
    }

    public void DrawMenu(float time, InputSystem input)
    {
        _lineBatch.Begin();

        const float titleScale = 2.8f;
        const float rowScale = 1.6f;
        const float statusScale = 1.5f;
        const float rowSpacing = 34f;
        var cx = GameConstants.WorldWidth * 0.5f;
        var startY = 72f;

        VectorFont.DrawText(_lineBatch, "VECTOR SPACE FIGHT", new Vector2(cx, startY),
            titleScale, Color.White * 0.85f, HorizontalAlign.Center);
        startY += 56f;

        for (var i = 0; i < PlayerRoster.Count; i++)
        {
            var rowY = startY + i * rowSpacing;
            var claimed = input.IsSlotAssigned(i);
            var identified = input.IsSlotColorIdentified(i);
            var pulse = claimed
                ? MathF.Max(input.GetSlotActivityPulse(i), identified ? 0.35f : 0.2f)
                : 0f;
            var textColor = input.GetSlotTextColor(i);
            var indicatorColor = claimed
                ? (identified ? textColor : PlayerPalette.MenuSlotGray) * (0.55f + pulse * 0.35f)
                : PlayerPalette.MenuSlotGray * 0.28f;

            DrawClaimIndicator(new Vector2(cx - 220f, rowY), pulse, indicatorColor);

            string label;
            if (claimed)
            {
                var status = input.GetSlotDisplayLabel(i);
                if (string.IsNullOrWhiteSpace(status))
                    status = "Identifying...";

                var rosterTag = input.GetAssignedPlayerIndex(i) is int rosterIndex
                    ? $" - {PlayerRoster.ColorNames[rosterIndex]}"
                    : string.Empty;
                var menuTag = input.IsMenuControllerSlot(i) ? " - MENU" : string.Empty;
                label = $"SLOT {i + 1} - {status}{rosterTag}{menuTag}";
            }
            else
            {
                label = $"SLOT {i + 1} - MOVE TO CLAIM";
            }

            VectorFont.DrawText(_lineBatch, label, new Vector2(cx - 180f, rowY), rowScale, textColor);
        }

        string message;
        Color messageColor;
        if (input.ClaimedControllerCount == 0)
        {
            message = "MOVE A CONTROLLER TO TAKE CHARGE OF THIS MENU";
            messageColor = Color.White * 0.7f;
        }
        else if (input.CanStartGame())
        {
            message = "MENU CONTROLLER: BUTTON 1 TO START";
            messageColor = input.GetSlotTextColor(0);
        }
        else
        {
            message = "MOVE TO CLAIM - WAITING FOR PLAYERS";
            messageColor = Color.White * 0.55f;
        }

        VectorFont.DrawText(_lineBatch, message, new Vector2(cx, startY + PlayerRoster.Count * rowSpacing + 24f),
            statusScale, messageColor, HorizontalAlign.Center);

        _lineBatch.Flush(CreateViewProjection());
    }

    private void DrawClaimIndicator(Vector2 center, float pulse, Color color)
    {
        pulse = MathHelper.Clamp(pulse, 0f, 1f);
        if (pulse <= 0.01f)
        {
            _lineBatch.DrawCircle(center, 6f, color * 0.35f, 16);
            return;
        }

        var bright = color * (0.55f + pulse * 0.45f);
        _lineBatch.DrawCircle(center, 6f + pulse * 4f, bright, 24);
        for (var i = 0; i < 4; i++)
        {
            var angle = i * MathHelper.PiOver2 + pulse * MathHelper.TwoPi;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            _lineBatch.DrawLine(center, center + dir * (10f + pulse * 8f), bright);
        }
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

        if (ship.ShieldActive && ship.HasShieldProtection)
            _lineBatch.DrawCircle(position, GameConstants.ShieldRadius, ship.Color);
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

    public void DrawShaderTuningHud(RenderSettings settings)
    {
        if (settings.HudVisibleTimer <= 0f)
            return;

        _lineBatch.Begin();

        const float scale = 1.35f;
        const float lineHeight = 14f;
        var origin = new Vector2(18f, 18f);
        var onColor = Color.Lime * 0.9f;
        onColor.A = 255;
        var offColor = Color.Gray * 0.55f;
        offColor.A = 255;
        var headerColor = Color.White * 0.75f;
        headerColor.A = 255;

        VectorFont.DrawText(_lineBatch, "SHADER TUNING", origin, scale, headerColor);
        float y = origin.Y + lineHeight * 1.6f;

        DrawToggleRow("F1 BLOOM", settings.Bloom, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F2 NEON GLOW", settings.NeonGlow, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F3 NEON CORE", settings.NeonCore, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F4 TUBE EXPAND", settings.NeonTubeExpand, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F5 NEON TUBES", settings.NeonTubes, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F6 SCANLINES", settings.Scanlines, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F7 PHOSPHOR", settings.PhosphorMask, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F8 VIGNETTE", settings.Vignette, ref y, scale, lineHeight, onColor, offColor);
        DrawToggleRow("F9 NOISE", settings.FilmNoise, ref y, scale, lineHeight, onColor, offColor);
        VectorFont.DrawText(_lineBatch, $"[ ] BLOOM {settings.BloomIntensity:0.00}", new Vector2(origin.X, y), scale, headerColor);
        y += lineHeight;
        VectorFont.DrawText(_lineBatch, $"- + GLOW {settings.NeonGlowIntensity:0.0}", new Vector2(origin.X, y), scale, headerColor);
        y += lineHeight;
        VectorFont.DrawText(_lineBatch, $"TUBE {settings.NeonTubeWidth:0.00}  , .", new Vector2(origin.X, y), scale, headerColor);
        y += lineHeight;
        VectorFont.DrawText(_lineBatch, "F10 NEON PRESET", new Vector2(origin.X, y), scale, headerColor);

        _lineBatch.Flush(CreateViewProjection());
    }

    private void DrawToggleRow(string label, bool enabled, ref float y, float scale, float lineHeight,
        Color onColor, Color offColor)
    {
        VectorFont.DrawText(_lineBatch, label, new Vector2(18f, y), scale, enabled ? onColor : offColor);
        y += lineHeight;
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
