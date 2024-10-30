cbuffer UBO : register(b0, space2)
{
    float ubo_time : packoffset(c0);
};

RWTexture2D<unorm float4> OutImage : register(u0, space1);

[numthreads(8, 8, 1)]
void main(uint3 GlobalInvocationID : SV_DispatchThreadID)
{
    float2 size;
    OutImage.GetDimensions(size.x, size.y);
    float2 coord = float2(GlobalInvocationID.xy);
    float2 uv = coord / size;
    float3 col = 0.5f.xxx + (cos((ubo_time.xxx + uv.xyx) + float3(0.0f, 2.0f, 4.0f)) * 0.5f);
    OutImage[int2(coord)] = float4(col, 1.0f);
}
