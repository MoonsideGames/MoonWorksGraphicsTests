#version 450

layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 TexCoord;

layout (location = 0) out vec4 outColor;

layout(binding = 0, set = 0) uniform sampler2D Sampler;

void main()
{
	outColor = texture(Sampler, TexCoord);
	gl_Position = vec4(Position, 1);
}
