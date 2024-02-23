using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TexturedAnimatedQuadGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture texture;
		private Sampler sampler;

		private float t;

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
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadWithMatrix.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadWithMultiplyColor.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.AttachmentInfo.ColorAttachmentDescriptions[0].BlendState = ColorAttachmentBlendState.AlphaBlend;
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create<TransformVertexUniform>(vertShaderModule, "main", 0);
			pipelineCreateInfo.FragmentShaderInfo = GraphicsShaderInfo.Create<FragmentUniforms>(fragShaderModule, "main", 1);
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(0.5f, -0.5f, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 1)),
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0, 1, 2,
					0, 2, 3,
				],
				BufferUsageFlags.Index
			);

			texture = resourceUploader.CreateTexture2D(TestUtils.GetTexturePath("ravioli.png"));

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta)
		{
			t += (float) delta.TotalSeconds;
		}

		protected override void Draw(double alpha)
		{
			TransformVertexUniform vertUniforms;
			FragmentUniforms fragUniforms;

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));

				// Top-left
				vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(-0.5f, -0.5f, 0)));
				fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Sin(t) * 0.5f, 1f, 1f));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.PushFragmentShaderUniforms(fragUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);

				// Top-right
				vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ((2 * System.MathF.PI) - t) * Matrix4x4.CreateTranslation(new Vector3(0.5f, -0.5f, 0)));
				fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Cos(t) * 0.5f, 1f, 1f));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.PushFragmentShaderUniforms(fragUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);

				// Bottom-left
				vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(-0.5f, 0.5f, 0)));
				fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Sin(t) * 0.2f, 1f, 1f));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.PushFragmentShaderUniforms(fragUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);

				// Bottom-right
				vertUniforms = new TransformVertexUniform(Matrix4x4.CreateRotationZ(t) * Matrix4x4.CreateTranslation(new Vector3(0.5f, 0.5f, 0)));
				fragUniforms = new FragmentUniforms(new Vector4(1f, 0.5f + System.MathF.Cos(t) * 1f, 1f, 1f));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.PushFragmentShaderUniforms(fragUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);

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
