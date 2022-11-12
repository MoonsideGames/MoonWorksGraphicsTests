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
		private Buffer cwVertexBuffer;
		private Buffer ccwVertexBuffer;

		private bool useClockwiseWinding;

		public CullFaceGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press A to toggle the winding order of the triangles (default is counter-clockwise)");

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColorVert.spv"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.spv"));

			// Create the graphics pipelines
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = new VertexInputState(
				VertexBinding.Create<PositionColorVertex>(),
				VertexAttribute.Create<PositionColorVertex>("Position", 0),
				VertexAttribute.Create<PositionColorVertex>("Color", 1)
			);

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
			cwVertexBuffer = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 3);
			ccwVertexBuffer = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 3);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				cwVertexBuffer,
				new PositionColorVertex[]
				{
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
				}
			);
			cmdbuf.SetBufferData(
				ccwVertexBuffer,
				new PositionColorVertex[]
				{
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),
				}
			);
			GraphicsDevice.Submit(cmdbuf);
			GraphicsDevice.Wait();
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (Inputs.Keyboard.IsPressed(Input.KeyCode.A))
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

				if (useClockwiseWinding)
				{
					cmdbuf.BindVertexBuffers(cwVertexBuffer);
				}
				else
				{
					cmdbuf.BindVertexBuffers(ccwVertexBuffer);
				}

				cmdbuf.SetViewport(new Viewport(0, 0, 213, 240));
				cmdbuf.BindGraphicsPipeline(CW_CullNonePipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

				cmdbuf.SetViewport(new Viewport(213, 0, 213, 240));
				cmdbuf.BindGraphicsPipeline(CW_CullFrontPipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

				cmdbuf.SetViewport(new Viewport(426, 0, 213, 240));
				cmdbuf.BindGraphicsPipeline(CW_CullBackPipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

				cmdbuf.SetViewport(new Viewport(0, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullNonePipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

				cmdbuf.SetViewport(new Viewport(213, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullFrontPipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

				cmdbuf.SetViewport(new Viewport(426, 240, 213, 240));
				cmdbuf.BindGraphicsPipeline(CCW_CullBackPipeline);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);

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
