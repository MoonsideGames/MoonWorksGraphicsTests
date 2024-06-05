using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests
{
	class BasicStencilGame : Example
	{
		private GraphicsPipeline maskerPipeline;
		private GraphicsPipeline maskeePipeline;
		private GpuBuffer vertexBuffer;
		private Texture depthStencilTexture;

		public override void Init(Window window, GraphicsDevice graphicsDevice)
		{
			Window = window;
			GraphicsDevice = graphicsDevice;

			// Load the shaders
			Shader vertShaderModule = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("PositionColor.vert"),
				"main",
				ShaderStage.Vertex,
				ShaderFormat.SPIRV
			);
			Shader fragShaderModule = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("SolidColor.frag"),
				"main",
				ShaderStage.Fragment,
				ShaderFormat.SPIRV
			);

			// Create the graphics pipelines
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				Window.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.AttachmentInfo.HasDepthStencilAttachment = true;
			pipelineCreateInfo.AttachmentInfo.DepthStencilFormat = TextureFormat.D24_UNORM_S8_UINT;
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				StencilTestEnable = true,
				FrontStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Never,
					FailOp = StencilOp.Replace,
				},
				Reference = 1,
				WriteMask = 0xFF
			};
			maskerPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				StencilTestEnable = true,
				FrontStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Equal,
				},
				Reference = 0,
				CompareMask = 0xFF,
				WriteMask = 0
			};
			maskeePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			depthStencilTexture = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				TextureFormat.D24_UNORM_S8_UINT,
				TextureUsageFlags.DepthStencil
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

		public override void Update(System.TimeSpan delta) { }

		public override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
			if (swapchainTexture != null)
			{
				var renderPass = cmdbuf.BeginRenderPass(
					new DepthStencilAttachmentInfo(depthStencilTexture, true, new DepthStencilValue(0, 0), StoreOp.DontCare, StoreOp.DontCare),
					new ColorAttachmentInfo(swapchainTexture, false, Color.Black)
				);
				renderPass.BindGraphicsPipeline(maskerPipeline);
				renderPass.BindVertexBuffer(vertexBuffer);
				renderPass.DrawPrimitives(0, 1);
				renderPass.BindGraphicsPipeline(maskeePipeline);
				renderPass.DrawPrimitives(3, 1);
				cmdbuf.EndRenderPass(renderPass);
			}
			GraphicsDevice.Submit(cmdbuf);
		}

        public override void Destroy()
        {
            throw new System.NotImplementedException();
        }
    }
}
