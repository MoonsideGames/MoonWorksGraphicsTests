#version 450

layout (location = 0) in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;

layout(binding = 0, set = 1) uniform sampler2DArray Sampler;

layout (binding = 0, set = 3) uniform UniformBlock
{
	float depth;
};

void main()
{
	FragColor = texture(Sampler, vec3(TexCoord, depth));
}
