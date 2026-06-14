using Microsoft.Xna.Framework.Input;
using VectorSpaceFight.Game.Rendering;

namespace VectorSpaceFight.Game.Systems;

public sealed class ShaderTuningInput
{
    private readonly bool[] _previousKeys = new bool[256];

    public void Update(RenderSettings settings)
    {
        var keyboard = Keyboard.GetState();

        if (WasPressed(keyboard, Keys.F1))
            Toggle(settings, s => s.Bloom = !s.Bloom);

        if (WasPressed(keyboard, Keys.F2))
            Toggle(settings, s => s.NeonGlow = !s.NeonGlow);

        if (WasPressed(keyboard, Keys.F3))
            Toggle(settings, s => s.NeonCore = !s.NeonCore);

        if (WasPressed(keyboard, Keys.F4))
            Toggle(settings, s => s.NeonTubeExpand = !s.NeonTubeExpand);

        if (WasPressed(keyboard, Keys.F5))
            Toggle(settings, s => s.NeonTubes = !s.NeonTubes);

        if (WasPressed(keyboard, Keys.F6))
            Toggle(settings, s => s.Scanlines = !s.Scanlines);

        if (WasPressed(keyboard, Keys.F7))
            Toggle(settings, s => s.PhosphorMask = !s.PhosphorMask);

        if (WasPressed(keyboard, Keys.F8))
            Toggle(settings, s => s.Vignette = !s.Vignette);

        if (WasPressed(keyboard, Keys.F9))
            Toggle(settings, s => s.FilmNoise = !s.FilmNoise);

        if (WasPressed(keyboard, Keys.OemOpenBrackets))
            settings.BloomIntensity = MathF.Max(0.05f, settings.BloomIntensity - 0.05f);

        if (WasPressed(keyboard, Keys.OemCloseBrackets))
            settings.BloomIntensity = MathF.Min(1.5f, settings.BloomIntensity + 0.05f);

        if (WasPressed(keyboard, Keys.OemMinus))
            settings.NeonGlowIntensity = MathF.Max(0.2f, settings.NeonGlowIntensity - 0.1f);

        if (WasPressed(keyboard, Keys.OemPlus))
            settings.NeonGlowIntensity = MathF.Min(5f, settings.NeonGlowIntensity + 0.1f);

        if (WasPressed(keyboard, Keys.OemComma))
            Adjust(settings, s => s.NeonTubeWidth = MathF.Max(RenderSettings.NeonDefaultTubeWidth, s.NeonTubeWidth - 0.25f));

        if (WasPressed(keyboard, Keys.OemPeriod))
            Adjust(settings, s => s.NeonTubeWidth = MathF.Min(6f, s.NeonTubeWidth + 0.25f));

        if (WasPressed(keyboard, Keys.F10))
        {
            if (settings.IsNeonPresetActive())
                settings.ApplyAllOff();
            else
                settings.ApplyNeonDefaults();

            if (settings.ShowShaderHud)
                settings.MarkHudVisible();
        }

        for (int i = 0; i < _previousKeys.Length; i++)
            _previousKeys[i] = keyboard.IsKeyDown((Keys)i);
    }

    private bool WasPressed(KeyboardState keyboard, Keys key)
    {
        int index = (int)key;
        bool down = keyboard.IsKeyDown(key);
        bool pressed = down && !_previousKeys[index];
        return pressed;
    }

    private static void Toggle(RenderSettings settings, Action<RenderSettings> toggle)
    {
        toggle(settings);
        if (settings.ShowShaderHud)
            settings.MarkHudVisible();
    }

    private static void Adjust(RenderSettings settings, Action<RenderSettings> adjust)
    {
        adjust(settings);
        if (settings.ShowShaderHud)
            settings.MarkHudVisible();
    }
}
