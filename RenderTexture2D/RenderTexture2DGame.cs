using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class RenderTexture2DGame : Game
	{
		private GraphicsPipeline pipeline;
		private Buffer vertexBuffer;
		private Buffer indexBuffer;
		private Texture[] textures = new Texture[4];
		private Sampler sampler;

		public RenderTexture2DGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create(vertShaderModule, "main", 0);
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create sampler
			SamplerCreateInfo samplerCreateInfo = SamplerCreateInfo.PointClamp;
			sampler = new Sampler(GraphicsDevice, samplerCreateInfo);

			// Create and populate the GPU resources
			vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 16);
			indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

			for (int i = 0; i < textures.Length; i += 1)
			{
				textures[i] = Texture.CreateTexture2D(
					GraphicsDevice,
					MainWindow.Width / 4,
					MainWindow.Height / 4,
					TextureFormat.R8G8B8A8,
					TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
				);
			}

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				vertexBuffer,
				new PositionTextureVertex[]
				{
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(0, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(0, 0, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 0, 0), new Vector2(0, 1)),

					new PositionTextureVertex(new Vector3(0, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 0, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(0, 0, 0), new Vector2(0, 1)),

					new PositionTextureVertex(new Vector3(-1, 0, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(0, 0, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(0, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),

					new PositionTextureVertex(new Vector3(0, 0, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, 0, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(0, 1, 0), new Vector2(0, 1)),
				}
			);
			cmdbuf.SetBufferData(
				indexBuffer,
				new ushort[]
				{
					0, 1, 2,
					0, 2, 3,
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
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[0], Color.Red));
				cmdbuf.EndRenderPass();

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[1], Color.Blue));
				cmdbuf.EndRenderPass();

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[2], Color.Green));
				cmdbuf.EndRenderPass();

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[3], Color.Yellow));
				cmdbuf.EndRenderPass();

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);

				for (uint i = 0; i < textures.Length; i += 1)
				{
					cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(textures[i], sampler));
					cmdbuf.DrawIndexedPrimitives(4 * i, 0, 2, 0, 0);
				}

				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			RenderTexture2DGame game = new RenderTexture2DGame();
			game.Run();
		}
	}
}
