cbuffer UniformBlock : register(b0, space3)
{
    float zNear : packoffset(c0);
    float zFar : packoffset(c0.y);
};

Texture2D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

// Calculation taken from http://www.geeks3d.com/20091216/geexlab-how-to-visualize-the-depth-buffer-in-glsl/
float linearizeDepth(float originalDepth)
{
    return (2.0f * zNear) / ((zFar + zNear) - (originalDepth * (zFar - zNear)));
}

float4 main(float2 TexCoord : TEXCOORD0) : SV_Target0
{
    float d = Texture.Sample(Sampler, TexCoord).x;
    d = linearizeDepth(d);
    return float4(d, d, d, 1.0f);
}
