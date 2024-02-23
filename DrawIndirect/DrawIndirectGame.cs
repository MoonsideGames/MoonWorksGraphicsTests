using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
	class DrawIndirectGame : Game
	{
		private GraphicsPipeline graphicsPipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer drawBuffer;

		public DrawIndirectGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColor.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
			graphicsPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the vertex buffer
			var resourceInitializer = new ResourceInitializer(GraphicsDevice);

			vertexBuffer = resourceInitializer.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(-0.5f, -1, 0), Color.Blue),
					new PositionColorVertex(new Vector3(-1f, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(0f, 1, 0), Color.Red),

					new PositionColorVertex(new Vector3(.5f, -1, 0), Color.Blue),
					new PositionColorVertex(new Vector3(1f, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(0f, 1, 0), Color.Red),
				],
				BufferUsageFlags.Vertex
			);

			drawBuffer = resourceInitializer.CreateBuffer(
				[
					new IndirectDrawCommand(3, 1, 3, 0),
					new IndirectDrawCommand(3, 1, 0, 0),
				],
				BufferUsageFlags.Indirect
			);

			resourceInitializer.Upload();
			resourceInitializer.Dispose();
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.CornflowerBlue));
				cmdbuf.BindGraphicsPipeline(graphicsPipeline);
				cmdbuf.BindVertexBuffers(new BufferBinding(vertexBuffer, 0));
				cmdbuf.DrawPrimitivesIndirect(drawBuffer, 0, 2, (uint) Marshal.SizeOf<IndirectDrawCommand>());
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			DrawIndirectGame game = new DrawIndirectGame();
			game.Run();
		}
	}
}
