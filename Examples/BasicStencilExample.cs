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
		private Buffer VertexBuffer;
		private Texture DepthStencilTexture;

		public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
		{
			Window = window;
			GraphicsDevice = graphicsDevice;

			Window.SetTitle("BasicStencil");

			// Load the shaders
			Shader vertShaderModule = Shader.Create(
				GraphicsDevice,
				TestUtils.GetShaderPath("PositionColor.vert"),
				"main",
				new ShaderCreateInfo
				{
					Stage = ShaderStage.Vertex,
					Format = ShaderFormat.SPIRV
				}
			);
			Shader fragShaderModule = Shader.Create(
				GraphicsDevice,
				TestUtils.GetShaderPath("SolidColor.frag"),
				"main",
				new ShaderCreateInfo
				{
					Stage = ShaderStage.Fragment,
					Format = ShaderFormat.SPIRV
				}
			);

			// Create the graphics pipelines
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				Window.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.TargetInfo.HasDepthStencilTarget = true;
			pipelineCreateInfo.TargetInfo.DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat;
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(0);
			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				EnableStencilTest = true,
				FrontStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Never,
					FailOp = StencilOp.Replace,
					PassOp = StencilOp.Keep,
					DepthFailOp = StencilOp.Keep
				},
				BackStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Never,
					FailOp = StencilOp.Replace,
					PassOp = StencilOp.Keep,
					DepthFailOp = StencilOp.Keep
				},
				WriteMask = 0xFF
			};
			MaskerPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.DepthStencilState = new DepthStencilState
			{
				EnableStencilTest = true,
				FrontStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Equal,
					FailOp = StencilOp.Keep,
					PassOp = StencilOp.Keep,
					DepthFailOp = StencilOp.Keep
				},
				BackStencilState = new StencilOpState
				{
					CompareOp = CompareOp.Equal,
					FailOp = StencilOp.Keep,
					PassOp = StencilOp.Keep,
					DepthFailOp = StencilOp.Keep
				},
				CompareMask = 0xFF,
				WriteMask = 0
			};
			MaskeePipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the GPU resources
			DepthStencilTexture = Texture.Create2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				GraphicsDevice.SupportedDepthStencilFormat,
				TextureUsageFlags.DepthStencilTarget
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
					new ColorTargetInfo
					{
						Texture = swapchainTexture.Handle,
						LoadOp = LoadOp.Clear,
						ClearColor = Color.Black
					},
					new DepthStencilTargetInfo
					{
						Texture = DepthStencilTexture.Handle,
						LoadOp = LoadOp.Clear,
						ClearDepth = 0,
						StencilLoadOp = LoadOp.Clear,
						ClearStencil = 0,
						StoreOp = StoreOp.DontCare,
						StencilStoreOp = StoreOp.DontCare,
						Cycle = true
					}
				);
				renderPass.BindVertexBuffer(VertexBuffer);
				renderPass.SetStencilReference(1);
				renderPass.BindGraphicsPipeline(MaskerPipeline);
				renderPass.DrawPrimitives(3, 1, 0, 0);
				renderPass.SetStencilReference(0);
				renderPass.BindGraphicsPipeline(MaskeePipeline);
				renderPass.DrawPrimitives(3, 1, 3, 0);
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
