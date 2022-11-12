using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PositionVertex
	{
		public Vector3 Position;

		public PositionVertex(Vector3 position)
		{
			Position = position;
		}

		public override string ToString()
        {
			return Position.ToString();
		}
	}

    [StructLayout(LayoutKind.Sequential)]
	public struct PositionColorVertex
	{
		public Vector3 Position;
		public Color Color;

		public PositionColorVertex(Vector3 position, Color color)
		{
			Position = position;
			Color = color;
		}

		public override string ToString()
		{
			return Position + " | " + Color;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PositionTextureVertex
	{
		public Vector3 Position;
		public Vector2 TexCoord;

		public PositionTextureVertex(Vector3 position, Vector2 texCoord)
		{
			Position = position;
			TexCoord = texCoord;
		}

        public override string ToString()
        {
			return Position + " | " + TexCoord;
        }
    }
}
