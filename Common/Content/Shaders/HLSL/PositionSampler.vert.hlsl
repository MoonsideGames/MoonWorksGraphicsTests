Texture2D<float4> Texture : register(t0, space0);
SamplerState Sampler : register(s0, space0);

struct Input
{
    float3 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
};

struct Output
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
};

Output main(Input input)
{
    Output output;
    output.Position = float4(input.Position, 1.0f);
    output.Color = Texture.SampleLevel(Sampler, input.TexCoord, 0.0f);
    return output;
}
