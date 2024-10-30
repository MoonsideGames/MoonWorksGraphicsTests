cbuffer UniformBlock : register(b0, space1)
{
    float4x4 MatrixTransform : packoffset(c0);
};

struct Input
{
    float3 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

struct Output
{
    float4 Color : TEXCOORD0;
    float4 Position : SV_Position;
};

Output main(Input input)
{
    Output output;
    output.Position = mul(MatrixTransform, float4(input.Position, 1.0f));
    output.Color = input.Color;
    return output;
}
