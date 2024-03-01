using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class InstancingAndOffsetsGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;

		private bool useVertexOffset;
		private bool useIndexOffset;

		public InstancingAndOffsetsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Left to toggle vertex offset\nPress Right to toggle index offset");

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColorInstanced.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create and populate the vertex and index buffers
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Red),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Lime),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Blue),

					new PositionColorVertex(new Vector3(-1, 1, 0), Color.Orange),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.Green),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.Aqua),

					new PositionColorVertex(new Vector3(-1, 1, 0), Color.White),
					new PositionColorVertex(new Vector3(1, 1, 0), Color.White),
					new PositionColorVertex(new Vector3(0, -1, 0), Color.White),
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0, 1, 2,
					3, 4, 5,
				],
				BufferUsageFlags.Index
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				useVertexOffset = !useVertexOffset;
				Logger.LogInfo("Using vertex offset: " + useVertexOffset);
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
			{
				useIndexOffset = !useIndexOffset;
				Logger.LogInfo("Using index offset: " + useIndexOffset);
			}
		}

		protected override void Draw(double alpha)
		{
			uint vertexOffset = useVertexOffset ? 3u : 0;
			uint indexOffset = useIndexOffset ? 3u : 0;

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.DrawInstancedPrimitives(vertexOffset, indexOffset, 1, 16);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			InstancingAndOffsetsGame p = new InstancingAndOffsetsGame();
			p.Run();
		}
	}
}
