using System.Numerics;

namespace MoonWorksGraphicsTests;

public struct TransformVertexUniform
{
	public Matrix4x4 ViewProjection;

	public TransformVertexUniform(Matrix4x4 viewProjection)
	{
		ViewProjection = viewProjection;
	}
}
