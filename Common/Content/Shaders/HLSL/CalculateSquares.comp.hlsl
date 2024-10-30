RWByteAddressBuffer NumberBuffer : register(u0, space1);

[numthreads(8, 1, 1)]
void main(uint3 GlobalInvocationID : SV_DispatchThreadID)
{
    uint n = GlobalInvocationID.x;
    NumberBuffer.Store(n * 4 + 0, n * n);
}
