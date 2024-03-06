using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class ComputeUniformsGame : Game
	{
		private GraphicsPipeline drawPipeline;
		private Texture texture;
		private Sampler sampler;
		private GpuBuffer vertexBuffer;

		struct GradientTextureComputeUniforms
		{
			public uint groupCountX;
			public uint groupCountY;

			public GradientTextureComputeUniforms(uint w, uint h)
			{
				groupCountX = w;
				groupCountY = h;
			}
		}

		public ComputeUniformsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.Backend, 60, true)
		{
			// Create the compute pipeline that writes texture data
			ShaderModule gradientTextureComputeShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("GradientTexture.comp")
			);

			ComputePipeline gradientTextureComputePipeline = new ComputePipeline(
				GraphicsDevice,
				ComputeShaderInfo.Create<GradientTextureComputeUniforms>(gradientTextureComputeShaderModule, "main", 0, 1)
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

			GradientTextureComputeUniforms gradientUniforms = new GradientTextureComputeUniforms(
				texture.Width / 8,
				texture.Height / 8
			);

			cmdbuf.BeginComputePass();
			cmdbuf.BindComputePipeline(gradientTextureComputePipeline);
			cmdbuf.BindComputeTextures(new ComputeTextureBinding(texture, 0));
			cmdbuf.PushComputeShaderUniforms(gradientUniforms);
			cmdbuf.DispatchCompute(gradientUniforms.groupCountX, gradientUniforms.groupCountY, 1);
			cmdbuf.EndComputePass();

			GraphicsDevice.Submit(cmdbuf);
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
			ComputeUniformsGame game = new ComputeUniformsGame();
			game.Run();
		}
	}
}
