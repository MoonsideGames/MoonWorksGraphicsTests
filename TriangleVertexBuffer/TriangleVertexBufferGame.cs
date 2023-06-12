using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TriangleVertexBufferGame : Game
	{
		private GraphicsPipeline pipeline;
		private Buffer vertexBuffer;

		public TriangleVertexBufferGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColorVert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the vertex buffer
			vertexBuffer = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 3);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				vertexBuffer,
				new PositionColorVertex[]
				{
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Lime),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),
				}
			);
			GraphicsDevice.Submit(cmdbuf);
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			TriangleVertexBufferGame p = new TriangleVertexBufferGame();
			p.Run();
		}
	}
}
