using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class CullFaceGame : Game
	{
		private GraphicsPipeline CW_CullNonePipeline;
		private GraphicsPipeline CW_CullFrontPipeline;
		private GraphicsPipeline CW_CullBackPipeline;
		private GraphicsPipeline CCW_CullNonePipeline;
		private GraphicsPipeline CCW_CullFrontPipeline;
		private GraphicsPipeline CCW_CullBackPipeline;
		private GpuBuffer cwVertexBuffer;
		private GpuBuffer ccwVertexBuffer;

		private bool useClockwiseWinding;

		public CullFaceGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Down to toggle the winding order of the triangles (default is counter-clockwise)");

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColor.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			// Create the graphics pipelines
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();

			pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullNone;
			CW_CullNonePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullFront;
			CW_CullFrontPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullBack;
			CW_CullBackPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullNone;
			CCW_CullNonePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullFront;
			CCW_CullFrontPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullBack;
			CCW_CullBackPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the vertex buffers
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			cwVertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
				],
				BufferUsageFlags.Vertex
			);

			ccwVertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue)
				],
				BufferUsageFlags.Vertex
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
			{
				useClockwiseWinding = !useClockwiseWinding;
				Logger.LogInfo("Using clockwise winding: " + useClockwiseWinding);
			}
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));

				// Need to bind a pipeline before binding vertex buffers
				cmdbuf.BindGraphicsPipeline(CW_CullNonePipeline);
				if (useClockwiseWinding)
				{
					cmdbuf.BindVertexBuffers(cwVertexBuffer);
				}
				else
				{
					cmdbuf.BindVertexBuffers(ccwVertexBuffer);
				}

				cmdbuf.SetViewport(new Viewport(0, 0, 213, 240));
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.SetViewport(new Viewport(213, 0, 213, 240));
				cmdbuf.BindGraphicsPipeline(CW_CullFrontPipeline);
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.SetViewport(new Viewport(426, 0, 213, 240));
				cmdbuf.BindGraphicsPipeline(CW_CullBackPipeline);
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.SetViewport(new Viewport(0, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullNonePipeline);
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.SetViewport(new Viewport(213, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullFrontPipeline);
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.SetViewport(new Viewport(426, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullBackPipeline);
				cmdbuf.DrawPrimitives(0, 1);

				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			CullFaceGame game = new CullFaceGame();
			game.Run();
		}
	}
}
