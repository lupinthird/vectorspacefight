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

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;

    float2 texel = 1.0 / TextureSize;
    float3 color = SampleScene(uv);

    float3 bloomNear = GatherBloom(uv, texel, 1.5);
    float3 bloomFar = GatherBloom(uv, texel, 3.5);
    float bright = smoothstep(0.08, 0.32, Luma(color));
    color += (bloomNear * 0.48 + bloomFar * 0.32) * bright;

    float scanline = 0.92 + 0.08 * sin(uv.y * TextureSize.y * 3.14159);
    color *= scanline;

    float mask = sin(uv.x * TextureSize.x * 3.14159);
    color.r *= 0.97 + 0.03 * mask;
    color.g *= 0.97 + 0.03 * sin(mask + 2.094);
    color.b *= 0.97 + 0.03 * sin(mask + 4.188);

    float2 vigCoord = input.TexCoord - 0.5;
    float vignette = 1.0 - dot(vigCoord, vigCoord) * 1.15;
    color *= saturate(vignette);

    float noise = frac(sin(dot(uv * Time, float2(12.9898, 78.233))) * 43758.5453);
    color += (noise - 0.5) * 0.015;

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
