#pragma pack_matrix(row_major)

struct SpriteComputeData
{
    float3 position;
    float rotation;
    float2 scale;
    float4 color;
};

struct SpriteVertex
{
    float4 position;
    float2 texcoord;
    float4 color;
};

ByteAddressBuffer ComputeBuffer : register(t0, space0);
RWByteAddressBuffer VertexBuffer : register(u0, space1);

[numthreads(64, 1, 1)]
void main(uint3 GlobalInvocationID : SV_DispatchThreadID)
{
    uint n = GlobalInvocationID.x;

    SpriteComputeData currentSpriteData;
    currentSpriteData.position = asfloat(ComputeBuffer.Load3(n * 48 + 0));
    currentSpriteData.rotation = asfloat(ComputeBuffer.Load(n * 48 + 12));
    currentSpriteData.scale = asfloat(ComputeBuffer.Load2(n * 48 + 16));
    currentSpriteData.color = asfloat(ComputeBuffer.Load4(n * 48 + 32));

    float4x4 Scale = float4x4(
        float4(currentSpriteData.scale.x, 0.0f, 0.0f, 0.0f),
        float4(0.0f, currentSpriteData.scale.y, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(0.0f, 0.0f, 0.0f, 1.0f)
    );

    float c = cos(currentSpriteData.rotation);
    float s = sin(currentSpriteData.rotation);

    float4x4 Rotation = float4x4(
        float4(   c,    s, 0.0f, 0.0f),
        float4(  -s,    c, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(0.0f, 0.0f, 0.0f, 1.0f)
    );

    float4x4 Translation = float4x4(
        float4(1.0f, 0.0f, 0.0f, 0.0f),
        float4(0.0f, 1.0f, 0.0f, 0.0f),
        float4(0.0f, 0.0f, 1.0f, 0.0f),
        float4(currentSpriteData.position.x, currentSpriteData.position.y, currentSpriteData.position.z, 1.0f)
    );

    float4x4 Model = mul(Scale, mul(Rotation, Translation));

    float4 topLeft = float4(0.0f, 0.0f, 0.0f, 1.0f);
    float4 topRight = float4(1.0f, 0.0f, 0.0f, 1.0f);
    float4 bottomLeft = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 bottomRight = float4(1.0f, 1.0f, 0.0f, 1.0f);

    VertexBuffer.Store4((n * 4u) * 48 + 0, asuint(mul(topLeft, Model)));
    VertexBuffer.Store4(((n * 4u) + 1u) * 48 + 0, asuint(mul(topRight, Model)));
    VertexBuffer.Store4(((n * 4u) + 2u) * 48 + 0, asuint(mul(bottomLeft, Model)));
    VertexBuffer.Store4(((n * 4u) + 3u) * 48 + 0, asuint(mul(bottomRight, Model)));
    VertexBuffer.Store2((n * 4u) * 48 + 16, asuint(0.0f.xx));
    VertexBuffer.Store2(((n * 4u) + 1u) * 48 + 16, asuint(float2(1.0f, 0.0f)));
    VertexBuffer.Store2(((n * 4u) + 2u) * 48 + 16, asuint(float2(0.0f, 1.0f)));
    VertexBuffer.Store2(((n * 4u) + 3u) * 48 + 16, asuint(1.0f.xx));
    VertexBuffer.Store4((n * 4u) * 48 + 32, asuint(currentSpriteData.color));
    VertexBuffer.Store4(((n * 4u) + 1u) * 48 + 32, asuint(currentSpriteData.color));
    VertexBuffer.Store4(((n * 4u) + 2u) * 48 + 32, asuint(currentSpriteData.color));
    VertexBuffer.Store4(((n * 4u) + 3u) * 48 + 32, asuint(currentSpriteData.color));
}
