using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	[StructLayout(LayoutKind.Sequential)]
	public struct PositionVertex : IVertexType
	{
		public Vector3 Position;

		public PositionVertex(Vector3 position)
		{
			Position = position;
		}

		public static VertexElementFormat[] Formats { get; } = new VertexElementFormat[1]
		{
			VertexElementFormat.Vector3
		};

		public override string ToString()
        {
			return Position.ToString();
		}
	}

    [StructLayout(LayoutKind.Sequential)]
	public struct PositionColorVertex : IVertexType
	{
		public Vector3 Position;
		public Color Color;

		public PositionColorVertex(Vector3 position, Color color)
		{
			Position = position;
			Color = color;
		}

		public static VertexElementFormat[] Formats { get; } = new VertexElementFormat[2]
		{
			VertexElementFormat.Vector3,
			VertexElementFormat.Color
		};

		public override string ToString()
		{
			return Position + " | " + Color;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PositionTextureVertex : IVertexType
	{
		public Vector3 Position;
		public Vector2 TexCoord;

		public PositionTextureVertex(Vector3 position, Vector2 texCoord)
		{
			Position = position;
			TexCoord = texCoord;
		}

		public static VertexElementFormat[] Formats { get; } = new VertexElementFormat[2]
		{
			VertexElementFormat.Vector3,
			VertexElementFormat.Vector2
		};

        public override string ToString()
        {
			return Position + " | " + TexCoord;
        }
    }
}
