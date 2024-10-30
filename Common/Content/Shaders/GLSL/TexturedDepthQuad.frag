#version 450

layout (location = 0) in vec2 TexCoord;
layout (location = 0) out vec4 FragColor;

layout(set = 2, binding = 0) uniform sampler2D Sampler;

layout (set = 3, binding = 0) uniform UniformBlock
{
	float zNear;
	float zFar;
};

// Calculation taken from http://www.geeks3d.com/20091216/geexlab-how-to-visualize-the-depth-buffer-in-glsl/
float linearizeDepth(float originalDepth)
{
	return (2.0 * zNear) / (zFar + zNear - originalDepth * (zFar - zNear));
}

void main()
{
	float d = texture(Sampler, TexCoord).r;
	d = linearizeDepth(d);
	FragColor = vec4(d, d, d, 1.0);
}
