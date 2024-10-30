static float4 gl_Position;
static int gl_VertexIndex;
static float4 outColor;

struct SPIRV_Cross_Input
{
    uint gl_VertexIndex : SV_VertexID;
};

struct SPIRV_Cross_Output
{
    float4 outColor : TEXCOORD0;
    float4 gl_Position : SV_Position;
};

void vert_main()
{
    float2 pos;
    if (gl_VertexIndex == 0)
    {
        pos = (-1.0f).xx;
        outColor = float4(1.0f, 0.0f, 0.0f, 1.0f);
    }
    else
    {
        if (gl_VertexIndex == 1)
        {
            pos = float2(1.0f, -1.0f);
            outColor = float4(0.0f, 1.0f, 0.0f, 1.0f);
        }
        else
        {
            if (gl_VertexIndex == 2)
            {
                pos = float2(0.0f, 1.0f);
                outColor = float4(0.0f, 0.0f, 1.0f, 1.0f);
            }
        }
    }
    gl_Position = float4(pos, 0.0f, 1.0f);
}

SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
{
    gl_VertexIndex = int(stage_input.gl_VertexIndex);
    vert_main();
    SPIRV_Cross_Output stage_output;
    stage_output.gl_Position = gl_Position;
    stage_output.outColor = outColor;
    return stage_output;
}
