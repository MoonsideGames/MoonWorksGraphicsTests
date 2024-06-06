#version 450

layout (location = 0) in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;

layout(set = 2, binding = 0) uniform sampler3D Sampler;

layout (set = 3, binding = 0) uniform UniformBlock
{
	float depth;
};

void main()
{
	FragColor = texture(Sampler, vec3(TexCoord, depth));
}
