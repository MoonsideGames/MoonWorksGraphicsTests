// Code derived from: https://www.youtube.com/watch?v=f4s1h2YETNY

cbuffer UniformBlock : register(b0, space3)
{
    float time;
    float2 resolution;
};

float3 palette( float t ) {
    float3 a = float3(0.0, 0.5, 1.0);
    float3 b = float3(1.0, 0.5, 0.25);
    float3 c = float3(1.5, 0.5, 0.0);
    float3 d = float3(0.263,0.416,0.557);

    return a + b*cos( 6.28318*(c*t+d) );
}

// Thanks IQ
float eqTri(in float2 p, in float r)
{
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.x = p.x + r/k;
    if( p.x+k*p.y>0.0 ) p = float2(p.x-k*p.y,-k*p.x-p.y)/2.0;
    p.x -= clamp( p.x, -2.0*r, 0.0 );
    return -length(p)*sign(p.y);
}

float4 main(float4 Position : SV_Position) : SV_Target0
{
    float2 uv = (Position.xy * 2.0 - resolution) / resolution.y;
    float2 uv0 = uv;
    float3 finalColor = float3(0.0, 0.0, 0.0);

    for (float i = 0.0; i < 4.0; i++) {
        uv = frac(uv * 1.5) - 0.5;

        float d = length(uv) * exp(-length(uv0));

        float3 col = palette(length(uv0) + i*.4 + time*.4);

        d = sin(d*8. + time)/8.;
        d = abs(d);

        d = pow(0.01 / d, 1.2);

        finalColor += col * d;
    }

    return float4(finalColor, 1.0);
}
