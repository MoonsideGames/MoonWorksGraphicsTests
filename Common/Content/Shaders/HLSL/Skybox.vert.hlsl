cbuffer UBO : register(b0, space1)
{
    float4x4 ubo_ViewProjection : packoffset(c0);
};

struct Input
{
    float3 inPos : TEXCOORD0;
};

struct Output
{
    float4 Position : SV_Position;
    float3 vPos : TEXCOORD0;
};

Output main(Input input)
{
    Output output;
    output.vPos = input.inPos;
    output.Position = mul(ubo_ViewProjection, float4(input.inPos, 1.0f));
    return output;
}
