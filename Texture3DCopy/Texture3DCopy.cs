using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class RenderTexture3DGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture rt;
		private Texture texture3D;
		private Sampler sampler;

		private float t;
		private Color[] colors = new Color[]
		{
			Color.Red,
			Color.Green,
			Color.Blue,
		};

		struct FragUniform
		{
			public float Depth;

			public FragUniform(float depth)
			{
				Depth = depth;
			}
		}

		public RenderTexture3DGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.PreferredBackends, 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad3D.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.FragmentShaderInfo = GraphicsShaderInfo.Create<FragUniform>(fragShaderModule, "main", 1);
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create samplers
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0, 1, 2,
					0, 2, 3,
				],
				BufferUsageFlags.Index
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();

			rt = Texture.CreateTexture2DArray(
				GraphicsDevice,
				16,
				16,
				(uint) colors.Length,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
			);

			texture3D = new Texture(GraphicsDevice, new TextureCreateInfo
			{
				Width = 16,
				Height = 16,
				Depth = 3,
				IsCube = false,
				LayerCount = 1,
				LevelCount = 1,
				SampleCount = SampleCount.One,
				Format = TextureFormat.R8G8B8A8,
				UsageFlags = TextureUsageFlags.Sampler
			});

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			// Clear each layer slice of the RT to a different color
			for (uint i = 0; i < colors.Length; i += 1)
			{
				ColorAttachmentInfo attachmentInfo = new ColorAttachmentInfo
				{
					TextureSlice = new TextureSlice
					{
						Texture = rt,
						Layer = i,
						MipLevel = 0
					},
					ClearColor = colors[i],
					LoadOp = LoadOp.Clear,
					StoreOp = StoreOp.Store
				};
				cmdbuf.BeginRenderPass(attachmentInfo);
				cmdbuf.EndRenderPass();
			}

			// Copy each layer slice to a different 3D depth
			cmdbuf.BeginCopyPass();
			for (var i = 0; i < 3; i += 1)
			{
				cmdbuf.CopyTextureToTexture(
					new TextureRegion
					{
						TextureSlice = new TextureSlice
						{
							Texture = rt,
							Layer = (uint) i,
							MipLevel = 0
						},
						X = 0,
						Y = 0,
						Z = 0,
						Width = 16,
						Height = 16,
						Depth = 1
					},
					new TextureRegion
					{
						TextureSlice = new TextureSlice
						{
							Texture = texture3D,
							Layer = 0,
							MipLevel = 0
						},
						X = 0,
						Y = 0,
						Z = (uint) i,
						Width = 16,
						Height = 16,
						Depth = 1
					},
					WriteOptions.SafeOverwrite
				);
			}
			cmdbuf.EndCopyPass();

			GraphicsDevice.Submit(cmdbuf);
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			t += 0.01f;
			FragUniform fragUniform = new FragUniform(t);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.Cycle, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture3D, sampler));
				cmdbuf.PushFragmentShaderUniforms(fragUniform);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			RenderTexture3DGame game = new RenderTexture3DGame();
			game.Run();
		}
	}
}
