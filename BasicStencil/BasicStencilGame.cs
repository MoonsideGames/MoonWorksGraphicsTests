using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class BasicStencilGame : Game
	{
		private GraphicsPipeline maskerPipeline;
		private GraphicsPipeline maskeePipeline;
		private GpuBuffer vertexBuffer;
		private Texture depthStencilTexture;

		public BasicStencilGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColor.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			// Create the graphics pipelines
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.AttachmentInfo.HasDepthStencilAttachment = true;
			pipelineCreateInfo.AttachmentInfo.DepthStencilFormat = TextureFormat.D16S8;
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				StencilTestEnable = true,
				StencilState = new StencilOpState
				{
					Reference = 1,
					WriteMask = 0xFF,
					CompareOp = CompareOp.Never,
					FailOp = StencilOp.Replace,
				}
			};
			maskerPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				StencilTestEnable = true,
				StencilState = new StencilOpState
				{
					Reference = 0,
					CompareMask = 0xFF,
					WriteMask = 0,
					CompareOp = CompareOp.Equal,
				}
			};
			maskeePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			depthStencilTexture = Texture.CreateTexture2D(
				GraphicsDevice,
				MainWindow.Width,
				MainWindow.Height,
				TextureFormat.D16S8,
				TextureUsageFlags.DepthStencilTarget
			);

			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(-0.5f, 0.5f, 0), Color.Yellow),
					new PositionColorVertex(new Vector3(0.5f, 0.5f, 0), Color.Yellow),
					new PositionColorVertex(new Vector3(0, -0.5f, 0), Color.Yellow),

					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Lime),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),
				],
				BufferUsageFlags.Vertex
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
				cmdbuf.BeginRenderPass(
					new DepthStencilAttachmentInfo(depthStencilTexture, WriteOptions.SafeDiscard, new DepthStencilValue(0, 0), StoreOp.DontCare, StoreOp.DontCare),
					new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black)
				);
				cmdbuf.BindGraphicsPipeline(maskerPipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.DrawPrimitives(0, 1);
				cmdbuf.BindGraphicsPipeline(maskeePipeline);
				cmdbuf.DrawPrimitives(3, 1);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			BasicStencilGame p = new BasicStencilGame();
			p.Run();
		}
	}
}
