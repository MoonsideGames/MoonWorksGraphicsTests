#version 450

layout(location = 0) in vec3 inPos;
layout(location = 0) out vec3 vPos;

layout(set = 1, binding = 0) uniform UBO
{
	mat4 ViewProjection;
} ubo;

void main()
{
    vPos = inPos;
    gl_Position = ubo.ViewProjection * vec4(inPos, 1.0);
}
