#version 450

layout(location = 0) in vec3 TexCoord;
layout(location = 0) out vec4 FragColor;

layout(set = 2, binding = 0) uniform samplerCube SkyboxSampler;

void main()
{
    FragColor = texture(SkyboxSampler, TexCoord);
}
