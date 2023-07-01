#version 450

layout (location = 0) in vec3 Position;
layout (location = 1) in vec4 Color;

layout (location = 0) out vec4 outColor;

layout (binding = 0, set = 2) uniform UniformBlock
{
	mat4x4 MatrixTransform;
};

void main()
{
	outColor = Color;
	gl_Position = MatrixTransform * vec4(Position, 1);
}
