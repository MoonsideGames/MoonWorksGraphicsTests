using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class BasicComputeGame : Game
	{
		private GraphicsPipeline drawPipeline;
		private Texture texture;
		private Sampler sampler;
		private GpuBuffer vertexBuffer;

		public BasicComputeGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.PreferredBackends, 60, true)
		{
			// Create the compute pipeline that writes texture data
			ShaderModule fillTextureComputeShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("FillTexture.comp")
			);

			ComputePipeline fillTextureComputePipeline = new ComputePipeline(
				GraphicsDevice,
				ComputeShaderInfo.Create(fillTextureComputeShaderModule, "main", 0, 1)
			);

			// Create the compute pipeline that calculates squares of numbers
			ShaderModule calculateSquaresComputeShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("CalculateSquares.comp")
			);

			ComputePipeline calculateSquaresComputePipeline = new ComputePipeline(
				GraphicsDevice,
				ComputeShaderInfo.Create(calculateSquaresComputeShaderModule, "main", 1, 0)
			);

			// Create the graphics pipeline
			ShaderModule vertShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("TexturedQuad.vert")
			);

			ShaderModule fragShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("TexturedQuad.frag")
			);

			GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			drawPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			drawPipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;

			drawPipeline = new GraphicsPipeline(
				GraphicsDevice,
				drawPipelineCreateInfo
			);

			// Create buffers and textures
			uint[] squares = new uint[64];
			GpuBuffer squaresBuffer = GpuBuffer.Create<uint>(
				GraphicsDevice,
				BufferUsageFlags.Compute,
				(uint) squares.Length
			);

			TransferBuffer transferBuffer = new TransferBuffer(
				GraphicsDevice,
				squaresBuffer.Size
			);

			texture = Texture.CreateTexture2D(
				GraphicsDevice,
				MainWindow.Width,
				MainWindow.Height,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.Compute | TextureUsageFlags.Sampler
			);

			sampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

			// Upload GPU resources and dispatch compute work
			var resourceUploader = new ResourceUploader(GraphicsDevice);
			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
				],
				BufferUsageFlags.Vertex
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			cmdbuf.BeginComputePass();

			// This should result in a bright yellow texture!
			cmdbuf.BindComputePipeline(fillTextureComputePipeline);
			cmdbuf.BindComputeTextures(new ComputeTextureBinding(texture, WriteOptions.SafeOverwrite));
			cmdbuf.DispatchCompute(texture.Width / 8, texture.Height / 8, 1);

			// This calculates the squares of the first N integers!
			cmdbuf.BindComputePipeline(calculateSquaresComputePipeline);
			cmdbuf.BindComputeBuffers(new ComputeBufferBinding(squaresBuffer, WriteOptions.SafeOverwrite));
			cmdbuf.DispatchCompute((uint) squares.Length / 8, 1, 1);

			cmdbuf.EndComputePass();

			var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Print the squares!
			GraphicsDevice.DownloadFromBuffer(squaresBuffer, transferBuffer, TransferOptions.Overwrite);
			transferBuffer.GetData<uint>(squares, 0);
			Logger.LogInfo("Squares of the first " + squares.Length + " integers: " + string.Join(", ", squares));
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.CornflowerBlue));
				cmdbuf.BindGraphicsPipeline(drawPipeline);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.DrawPrimitives(0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			BasicComputeGame game = new BasicComputeGame();
			game.Run();
		}
	}
}
