using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests
{
	class BasicStencilExample : Example
	{
		private GraphicsPipeline MaskerPipeline;
		private GraphicsPipeline MaskeePipeline;
		private GpuBuffer VertexBuffer;
		private Texture DepthStencilTexture;

		public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
		{
			Window = window;
			GraphicsDevice = graphicsDevice;

			Window.SetTitle("BasicStencil");

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
			MaskerPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

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
			MaskeePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			DepthStencilTexture = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				TextureFormat.D24_UNORM_S8_UINT,
				TextureUsageFlags.DepthStencil
			);

			var resourceUploader = new ResourceUploader(GraphicsDevice);

			VertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(-0.5f,  -0.5f, 0), Color.Yellow),
					new PositionColorVertex(new Vector3( 0.5f,  -0.5f, 0), Color.Yellow),
					new PositionColorVertex(new Vector3(    0,   0.5f, 0), Color.Yellow),

					new PositionColorVertex(new Vector3(-1, -1, 0), Color.Red),
					new PositionColorVertex(new Vector3( 1, -1, 0), Color.Lime),
					new PositionColorVertex(new Vector3( 0,  1, 0), Color.Blue),
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
					new DepthStencilAttachmentInfo(DepthStencilTexture, true, new DepthStencilValue(0, 0), StoreOp.DontCare, StoreOp.DontCare),
					new ColorAttachmentInfo(swapchainTexture, false, Color.Black)
				);
				renderPass.BindGraphicsPipeline(MaskerPipeline);
				renderPass.BindVertexBuffer(VertexBuffer);
				renderPass.DrawPrimitives(0, 1);
				renderPass.BindGraphicsPipeline(MaskeePipeline);
				renderPass.DrawPrimitives(3, 1);
				cmdbuf.EndRenderPass(renderPass);
			}
			GraphicsDevice.Submit(cmdbuf);
		}

        public override void Destroy()
        {
            MaskerPipeline.Dispose();
			MaskeePipeline.Dispose();
			VertexBuffer.Dispose();
			DepthStencilTexture.Dispose();
        }
    }
}
