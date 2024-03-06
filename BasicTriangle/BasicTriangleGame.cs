using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class BasicTriangleGame : Game
	{
		private GraphicsPipeline fillPipeline;
		private GraphicsPipeline linePipeline;

		private Viewport smallViewport = new Viewport(160, 120, 320, 240);
		private Rect scissorRect = new Rect(320, 240, 320, 240);

		private bool useWireframeMode;
		private bool useSmallViewport;
		private bool useScissorRect;

		public BasicTriangleGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.DefaultBackend, 60, true)
		{
			Logger.LogInfo("Press Left to toggle wireframe mode\nPress Down to toggle small viewport\nPress Right to toggle scissor rect");

			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangle.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			fillPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState.FillMode = FillMode.Line;
			linePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				useWireframeMode = !useWireframeMode;
				Logger.LogInfo("Using wireframe mode: " + useWireframeMode);
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
			{
				useSmallViewport = !useSmallViewport;
				Logger.LogInfo("Using small viewport: " + useSmallViewport);
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
			{
				useScissorRect = !useScissorRect;
				Logger.LogInfo("Using scissor rect: " + useScissorRect);
			}
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(useWireframeMode ? linePipeline : fillPipeline);

				if (useSmallViewport)
				{
					cmdbuf.SetViewport(smallViewport);
				}
				if (useScissorRect)
				{
					cmdbuf.SetScissor(scissorRect);
				}

				cmdbuf.DrawPrimitives(0, 1);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			BasicTriangleGame game = new BasicTriangleGame();
			game.Run();
		}
	}
}
