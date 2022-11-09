using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
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
	}
}
