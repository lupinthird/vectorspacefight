using VectorSpaceFight.Config;
using VectorSpaceFight.Game.Constants;

namespace VectorSpaceFight.Game.Rendering;

public sealed class RenderSettings
{
    public const float NeonDefaultGlowIntensity = 3.0f;
    public const float NeonDefaultTubeWidth = 0.5f;

    public bool Bloom { get; set; }
    public float BloomIntensity { get; set; } = GameConstants.BloomDefaultIntensity;

    public bool NeonGlow { get; set; } = true;
    public float NeonGlowIntensity { get; set; } = NeonDefaultGlowIntensity;

    public bool NeonCore { get; set; } = true;
    public bool NeonTubeExpand { get; set; }
    public bool NeonTubes { get; set; }

    public bool Scanlines { get; set; }
    public bool PhosphorMask { get; set; }
    public bool Vignette { get; set; }
    public bool FilmNoise { get; set; }

    public bool PostProcessEnabled { get; set; } = true;
    public bool ShowShaderHud { get; set; }

    public float LineIntensity { get; set; } = GameConstants.VectorLineIntensity;
    public float NeonTubeWidth { get; set; } = NeonDefaultTubeWidth;

    public float HudVisibleTimer { get; set; }

    public void MarkHudVisible() => HudVisibleTimer = 4f;

    public void Update(float dt)
    {
        if (HudVisibleTimer > 0f)
            HudVisibleTimer -= dt;
    }

    public bool IsNeonPresetActive() =>
        NeonGlow &&
        NeonCore &&
        !Bloom &&
        !NeonTubeExpand &&
        !NeonTubes &&
        !Scanlines &&
        !PhosphorMask &&
        !Vignette &&
        !FilmNoise;

    public void ApplyNeonDefaults()
    {
        PostProcessEnabled = true;
        Bloom = false;
        BloomIntensity = GameConstants.BloomDefaultIntensity;
        NeonGlow = true;
        NeonGlowIntensity = NeonDefaultGlowIntensity;
        NeonCore = true;
        NeonTubeExpand = false;
        NeonTubes = false;
        Scanlines = false;
        PhosphorMask = false;
        Vignette = false;
        FilmNoise = false;
        NeonTubeWidth = NeonDefaultTubeWidth;
    }

    public void ApplyAllOff()
    {
        Bloom = false;
        BloomIntensity = GameConstants.BloomDefaultIntensity;
        NeonGlow = false;
        NeonCore = false;
        NeonTubeExpand = false;
        NeonTubes = false;
        Scanlines = false;
        PhosphorMask = false;
        Vignette = false;
        FilmNoise = false;
        PostProcessEnabled = false;
    }

    public void ApplyFromConfig(RenderingSettings config)
    {
        PostProcessEnabled = config.PostProcessEnabled;
        ShowShaderHud = config.ShowShaderHud;
        Bloom = config.Bloom;
        BloomIntensity = config.BloomIntensity;
        NeonGlow = config.NeonGlow;
        NeonGlowIntensity = config.NeonGlowIntensity;
        NeonCore = config.NeonCore;
        NeonTubeExpand = config.NeonTubeExpand;
        NeonTubes = config.NeonTubes;
        Scanlines = config.Scanlines;
        PhosphorMask = config.PhosphorMask;
        Vignette = config.Vignette;
        FilmNoise = config.FilmNoise;
        LineIntensity = config.LineIntensity;
        NeonTubeWidth = config.NeonTubeWidth;
    }

    public static float AsFlag(bool enabled) => enabled ? 1f : 0f;
}
