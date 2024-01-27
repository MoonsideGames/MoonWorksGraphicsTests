#version 450

layout (location = 0) in vec3 Position;
layout (location = 1) in vec3 Translation;
layout (location = 2) in float Rotation;
layout (location = 3) in vec2 Scale;
layout (location = 4) in vec4 Color;
layout (location = 5) in vec2[4] UV;

layout (location = 0) out vec2 outTexCoord;
layout (location = 1) out vec4 outVertexColor;

layout (binding = 0, set = 2) uniform UniformBlock
{
	mat4x4 View;
	mat4x4 Projection;
};

void main()
{
	mat4 Scale = mat4(
		Scale.x, 0, 0, 0,
		0, Scale.y, 0, 0,
		0, 0,       1, 0,
		0, 0,       0, 1
	);
	float c = cos(Rotation);
	float s = sin(Rotation);
	mat4 Rotation = mat4(
		c, s, 0, 0,
		-s, c, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 1
	);
	mat4 Translation = mat4(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		Translation.x, Translation.y, Translation.z, 1
	);
	mat4 Model = Translation * Rotation * Scale;
	gl_Position = Projection * View * Model * vec4(Position, 1);
	outTexCoord = UV[gl_VertexIndex % 4];
	outVertexColor = Color;
}
