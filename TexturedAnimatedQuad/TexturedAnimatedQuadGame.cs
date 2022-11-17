using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TexturedAnimatedQuadGame : Game
	{
		private GraphicsPipeline pipeline;
		private Buffer vertexBuffer;
		private Buffer indexBuffer;
		private Texture texture;
		private Sampler sampler;

		private float t;

		[StructLayout(LayoutKind.Sequential)]
		private struct VertexUniforms
		{
			public Matrix4x4 TransformMatrix;

			public VertexUniforms(Matrix4x4 transformMatrix)
			{
				TransformMatrix = transformMatrix;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct FragmentUniforms
		{
			public Vector4 MultiplyColor;

			public FragmentUniforms(Vector4 multiplyColor)
			{
				MultiplyColor = multiplyColor;
			}
		}

		public TexturedAnimatedQuadGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadAnimatedVert.spv"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadAnimatedFrag.spv"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = new VertexInputState(
				VertexBinding.Create<PositionTextureVertex>(),
				VertexAttribute.Create<PositionTextureVertex>("Position", 0),
				VertexAttribute.Create<PositionTextureVertex>("TexCoord", 1)
			);
			pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create<VertexUniforms>(vertShaderModule, "main", 0);
			pipelineCreateInfo.FragmentShaderInfo = GraphicsShaderInfo.Create<FragmentUniforms>(fragShaderModule, "main", 1);
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
			indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				vertexBuffer,
				new PositionTextureVertex[]
				{
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
				}
			);
			cmdbuf.SetBufferData(
				indexBuffer,
				new ushort[]
				{
					0, 1, 2,
					0, 2, 3,
				}
			);
			texture = Texture.LoadPNG(GraphicsDevice, cmdbuf, TestUtils.GetTexturePath("ravioli.png"));
			GraphicsDevice.Submit(cmdbuf);
			GraphicsDevice.Wait();
		}

		protected override void Update(System.TimeSpan delta)
		{
			t += (float) delta.TotalSeconds;
		}

		protected override void Draw(double alpha)
		{
			VertexUniforms vertUniforms = new VertexUniforms(Matrix4x4.CreateRotationZ(t));
			FragmentUniforms fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Sin(t) * 0.5f, 1f, 1f));

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
				uint vertParamOffset = cmdbuf.PushVertexShaderUniforms(vertUniforms);
				uint fragParamOffset = cmdbuf.PushFragmentShaderUniforms(fragUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2, vertParamOffset, fragParamOffset);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			TexturedAnimatedQuadGame game = new TexturedAnimatedQuadGame();
			game.Run();
		}
	}
}
