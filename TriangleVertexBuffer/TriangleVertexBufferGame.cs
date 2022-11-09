using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TriangleVertexBufferGame : Game
	{
		private GraphicsPipeline pipeline;
		private ShaderModule vertShaderModule;
		private ShaderModule fragShaderModule;
		private Buffer vertexBuffer;

		public TriangleVertexBufferGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			vertShaderModule = new ShaderModule(GraphicsDevice, "Content/Shaders/Compiled/PositionColorVert.spv");
			fragShaderModule = new ShaderModule(GraphicsDevice, "Content/Shaders/Compiled/SolidColor.spv");

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(vertShaderModule, fragShaderModule);
			pipelineCreateInfo.VertexInputState = new VertexInputState(
				VertexBinding.Create<PositionColorVertex>(),
				VertexAttribute.Create<PositionColorVertex>("Position", 0),
				VertexAttribute.Create<PositionColorVertex>("Color", 1)
			);
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
			GraphicsDevice.Wait();
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
