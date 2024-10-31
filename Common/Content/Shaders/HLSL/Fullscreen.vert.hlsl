float4 main(uint VertexIndex : SV_VertexID) : SV_Position
{
    float2 texCoord = float2(float((VertexIndex << 1) & 2), float(VertexIndex & 2));
    return float4((texCoord * float2(2.0f, -2.0f)) + float2(-1.0f, 1.0f), 0.0f, 1.0f);
}
