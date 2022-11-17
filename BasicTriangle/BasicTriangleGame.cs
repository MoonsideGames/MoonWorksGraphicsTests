using MoonWorks;
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

		public BasicTriangleGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press A to toggle wireframe mode\nPress S to toggle small viewport\nPress D to toggle scissor rect");

			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangleVertices.spv"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.spv"));

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
			if (Inputs.Keyboard.IsPressed(Input.KeyCode.A))
			{
				useWireframeMode = !useWireframeMode;
				Logger.LogInfo("Using wireframe mode: " + useWireframeMode);
			}

			if (Inputs.Keyboard.IsPressed(Input.KeyCode.S))
			{
				useSmallViewport = !useSmallViewport;
				Logger.LogInfo("Using small viewport: " + useSmallViewport);
			}

			if (Inputs.Keyboard.IsPressed(Input.KeyCode.D))
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
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(useWireframeMode ? linePipeline : fillPipeline);

				if (useSmallViewport)
				{
					cmdbuf.SetViewport(smallViewport);
				}
				if (useScissorRect)
				{
					cmdbuf.SetScissor(scissorRect);
				}

				cmdbuf.DrawPrimitives(0, 1, 0, 0);
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
