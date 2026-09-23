// Shared helpers for the second set of world shaders: cheap hash noise (no textures, so each
// shader stands alone), fbm, HSV, and the scene's linear fog read from the globals URP sets.
#ifndef TUBITYX_WORLD_NOISE
#define TUBITYX_WORLD_NOISE

float wn_hash31(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float wn_noise3(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(wn_hash31(i + float3(0, 0, 0)), wn_hash31(i + float3(1, 0, 0)), f.x),
                     lerp(wn_hash31(i + float3(0, 1, 0)), wn_hash31(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(wn_hash31(i + float3(0, 0, 1)), wn_hash31(i + float3(1, 0, 1)), f.x),
                     lerp(wn_hash31(i + float3(0, 1, 1)), wn_hash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

// Four octaves, roughly [0, 0.94].
float wn_fbm3(float3 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int k = 0; k < 4; k++)
    {
        v += a * wn_noise3(p);
        p = p * 2.02 + 17.1;
        a *= 0.5;
    }
    return v;
}

float3 wn_hsv2rgb(float3 c)
{
    float3 p = abs(frac(c.xxx + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return c.z * lerp(float3(1, 1, 1), saturate(p - 1.0), c.y);
}

// Linear fog as the rest of the scene has it: 1 = clear, 0 = fully fogged.
float wn_fogFactor(float3 worldPos)
{
    float d = length(_WorldSpaceCameraPos - worldPos);
    return saturate(d * unity_FogParams.z + unity_FogParams.w);
}

#endif
