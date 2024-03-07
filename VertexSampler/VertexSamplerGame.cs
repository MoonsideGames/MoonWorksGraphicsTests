using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class VertexSamplerGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private Texture texture;
		private Sampler sampler;

		public VertexSamplerGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.PreferredBackends, 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionSampler.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.VertexShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(0.334f, 0)),
					new PositionTextureVertex(new Vector3(0, -1, 0), new Vector2(0.667f, 0)),
				],
				BufferUsageFlags.Vertex
			);

			texture = resourceUploader.CreateTexture2D(
				new Span<Color>([Color.Yellow, Color.Indigo, Color.HotPink]),
				3,
				1
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindVertexSamplers(new TextureSamplerBinding(texture, sampler));
				cmdbuf.DrawPrimitives(0, 1);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			VertexSamplerGame p = new VertexSamplerGame();
			p.Run();
		}
	}
}
