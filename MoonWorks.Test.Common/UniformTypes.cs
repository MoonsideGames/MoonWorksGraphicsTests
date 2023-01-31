using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
    public struct TransformVertexUniform
    {
        public Matrix4x4 ViewProjection;

        public TransformVertexUniform(Matrix4x4 viewProjection)
        {
            ViewProjection = viewProjection;
        }
    }
}
