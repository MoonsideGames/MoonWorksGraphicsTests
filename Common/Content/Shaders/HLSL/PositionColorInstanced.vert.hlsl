struct Input
{
    float3 Position : TEXCOORD0;
    float4 Color : TEXCOORD1;
    uint InstanceIndex : SV_InstanceID;
};

struct Output
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
};

Output main(Input input)
{
    float3 pos = (input.Position * 0.25f) - float3(0.75f, 0.75f, 0.0f);
    pos.x += (float(input.InstanceIndex % 4) * 0.5f);
    pos.y += (floor(float(input.InstanceIndex / 4)) * 0.5f);

    Output output;
    output.Position = float4(pos, 1.0f);
    output.Color = input.Color;
    return output;
}
