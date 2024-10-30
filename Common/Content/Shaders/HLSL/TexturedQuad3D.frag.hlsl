cbuffer UniformBlock : register(b0, space3)
{
    float depth : packoffset(c0);
};

Texture3D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

float4 main(float2 TexCoord : TEXCOORD0) : SV_Target0
{
    return Texture.Sample(Sampler, float3(TexCoord, depth));
}
