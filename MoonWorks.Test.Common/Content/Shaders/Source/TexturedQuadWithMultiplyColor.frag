#version 450

layout (location = 0) in vec2 TexCoord;

layout (location = 0) out vec4 FragColor;

layout(binding = 0, set = 1) uniform sampler2D Sampler;

layout (binding = 0, set = 3) uniform UniformBlock
{
	vec4 MultiplyColor;
};

void main()
{
	FragColor = MultiplyColor * texture(Sampler, TexCoord);
}
