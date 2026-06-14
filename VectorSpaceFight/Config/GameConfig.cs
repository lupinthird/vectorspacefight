using System.Text.Json;
using VectorSpaceFight.Game.Constants;
using VectorSpaceFight.Game.Rendering;

namespace VectorSpaceFight.Config;

public sealed class GameConfig
{
    public DisplaySettings Display { get; set; } = new();
    public RenderingSettings Rendering { get; set; } = new();

    public static GameConfig Load()
    {
        var path = GetConfigPath();
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var config = JsonSerializer.Deserialize<GameConfig>(json, options)
            ?? throw new InvalidOperationException($"Failed to parse config: {path}");

        config.Display.Clamp();
        config.Rendering.Clamp();
        return config;
    }

    public static string GetConfigPath()
    {
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var configPath = Path.Combine(configDir, "game.json");
        var defaultPath = Path.Combine(configDir, "game.default.json");

        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(configDir);
            if (File.Exists(defaultPath))
                File.Copy(defaultPath, configPath);
            else
                throw new FileNotFoundException("Config not found.", configPath);
        }

        return configPath;
    }
}

public sealed class DisplaySettings
{
    /// <summary>Lock game update/render pacing to 60 FPS.</summary>
    public bool Force60Hz { get; set; } = true;

    /// <summary>Enable vertical sync when supported by the display.</summary>
    public bool Vsync { get; set; } = true;

    /// <summary>Fullscreen borderless/windowed-fullscreen presentation.</summary>
    public bool Fullscreen { get; set; } = true;

    /// <summary>Back-buffer width. 0 = auto-fit 16:9 to the display.</summary>
    public int Width { get; set; }

    /// <summary>Back-buffer height. 0 = auto-fit 16:9 to the display.</summary>
    public int Height { get; set; }

    public void Clamp()
    {
        Width = Math.Max(0, Width);
        Height = Math.Max(0, Height);
    }
}

public sealed class RenderingSettings
{
    /// <summary>Master switch for the CRT post-process shader pass.</summary>
    public bool PostProcessEnabled { get; set; }

    /// <summary>Show the F-key shader tuning overlay (requires a keyboard).</summary>
    public bool ShowShaderHud { get; set; }

    public bool Bloom { get; set; }
    public float BloomIntensity { get; set; } = GameConstants.BloomDefaultIntensity;

    public bool NeonGlow { get; set; }
    public float NeonGlowIntensity { get; set; } = RenderSettings.NeonDefaultGlowIntensity;

    public bool NeonCore { get; set; }
    public bool NeonTubeExpand { get; set; }
    public bool NeonTubes { get; set; }

    public bool Scanlines { get; set; }
    public bool PhosphorMask { get; set; }
    public bool Vignette { get; set; }
    public bool FilmNoise { get; set; }

    public float LineIntensity { get; set; } = GameConstants.VectorLineIntensity;
    public float NeonTubeWidth { get; set; } = RenderSettings.NeonDefaultTubeWidth;

    public void Clamp()
    {
        BloomIntensity = Math.Clamp(BloomIntensity, 0.05f, 1.5f);
        NeonGlowIntensity = Math.Clamp(NeonGlowIntensity, 0.2f, 5f);
        LineIntensity = Math.Clamp(LineIntensity, 0.5f, 2f);
        NeonTubeWidth = Math.Clamp(NeonTubeWidth, 0.25f, 6f);
    }
}
