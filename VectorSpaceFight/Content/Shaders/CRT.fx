#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 TextureSize;
float Time;
float BloomIntensity = 0.2;
float NeonGlowIntensity = 3.0;

float BloomEnabled;
float NeonGlowEnabled;
float NeonCoreEnabled;
float NeonTubeExpandEnabled;
float ScanlinesEnabled;
float PhosphorEnabled;
float VignetteEnabled;
float NoiseEnabled;

texture SceneTexture;
sampler2D SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(float4 position : POSITION0, float2 texCoord : TEXCOORD0)
{
    VertexShaderOutput output;
    output.Position = position;
    output.TexCoord = texCoord;
    return output;
}

float Luma(float3 color)
{
    return dot(color, float3(0.299, 0.587, 0.114));
}

float3 SampleScene(float2 uv)
{
    return tex2D(SceneSampler, uv).rgb;
}

float3 GatherBloom(float2 uv, float2 texel, float spread)
{
    float2 offset = texel * spread;
    float3 bloom = 0.0;
    bloom += SampleScene(uv + float2(offset.x, 0));
    bloom += SampleScene(uv - float2(offset.x, 0));
    bloom += SampleScene(uv + float2(0, offset.y));
    bloom += SampleScene(uv - float2(0, offset.y));
    bloom += SampleScene(uv + offset);
    bloom += SampleScene(uv - offset);
    bloom += SampleScene(uv + float2(offset.x, -offset.y));
    bloom += SampleScene(uv + float2(-offset.x, offset.y));
    return bloom / 8.0;
}

float3 ExpandNeonTubes(float2 uv, float2 texel, float3 color)
{
    float3 expanded = color;
    float peakLuma = Luma(color);

    for (int x = -3; x <= 3; x++)
    {
        for (int y = -3; y <= 3; y++)
        {
            if (x == 0 && y == 0)
                continue;

            float2 offset = texel * float2(x, y) * 1.65;
            float3 sampleColor = SampleScene(uv + offset);
            float sampleLuma = Luma(sampleColor);
            if (sampleLuma <= peakLuma * 0.55)
                continue;

            float weight = sampleLuma / (peakLuma + 0.001);
            expanded = lerp(expanded, sampleColor, saturate(weight * 0.38));
            peakLuma = max(peakLuma, sampleLuma);
        }
    }

    return lerp(color, expanded, 0.88);
}

float3 BoostNeonCore(float3 color)
{
    float luma = Luma(color);
    float coreMask = smoothstep(0.1, 0.42, luma);
    float3 hue = color / (luma + 0.001);
    hue = normalize(max(hue, float3(0.001, 0.001, 0.001)));
    float3 hotCore = hue * saturate(luma * 2.1);
    hotCore = lerp(hotCore, float3(1.0, 1.0, 1.0), coreMask * 0.35);
    return lerp(color, hotCore, coreMask * 0.72);
}

float3 ApplyNeonGlow(float2 uv, float2 texel, float3 color)
{
    float3 source = SampleScene(uv);
    float emissive = smoothstep(0.025, 0.18, Luma(source));
    float3 glowNear = GatherBloom(uv, texel, 2.2);
    float3 glowMid = GatherBloom(uv, texel, 5.0);
    float3 glowFar = GatherBloom(uv, texel, 9.5);
    float3 glow = glowNear * 0.42 + glowMid * 0.34 + glowFar * 0.24;

    float sourceLuma = Luma(source);
    float3 tint = sourceLuma > 0.02 ? source / sourceLuma : float3(1.0, 1.0, 1.0);
    return color + glow * emissive * tint * NeonGlowIntensity;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float2 texel = 1.0 / TextureSize;
    float3 color = SampleScene(uv);

    color = lerp(color, ExpandNeonTubes(uv, texel, color), NeonTubeExpandEnabled);
    color = lerp(color, BoostNeonCore(color), NeonCoreEnabled);

    if (BloomEnabled > 0.5)
    {
        float3 bloomNear = GatherBloom(uv, texel, 1.5);
        float3 bloomFar = GatherBloom(uv, texel, 3.5);
        float bright = smoothstep(0.06, 0.28, Luma(color));
        color += (bloomNear * 0.48 + bloomFar * 0.32) * bright * BloomIntensity;
    }

    color = lerp(color, ApplyNeonGlow(uv, texel, color), NeonGlowEnabled);

    if (ScanlinesEnabled > 0.5)
    {
        float scanline = 0.92 + 0.08 * sin(uv.y * TextureSize.y * 3.14159);
        color *= scanline;
    }

    if (PhosphorEnabled > 0.5)
    {
        float mask = sin(uv.x * TextureSize.x * 3.14159);
        color.r *= 0.97 + 0.03 * mask;
        color.g *= 0.97 + 0.03 * sin(mask + 2.094);
        color.b *= 0.97 + 0.03 * sin(mask + 4.188);
    }

    if (VignetteEnabled > 0.5)
    {
        float2 vigCoord = input.TexCoord - 0.5;
        float vignette = 1.0 - dot(vigCoord, vigCoord) * 1.15;
        color *= saturate(vignette);
    }

    if (NoiseEnabled > 0.5)
    {
        float noise = frac(sin(dot(uv * Time, float2(12.9898, 78.233))) * 43758.5453);
        color += (noise - 0.5) * 0.015;
    }

    color = pow(saturate(color), 0.96);
    return float4(color, 1.0);
}

technique CRT
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
