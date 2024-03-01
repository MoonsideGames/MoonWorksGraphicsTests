using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class RenderTextureMipmapsGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture texture;

		private Sampler[] samplers = new Sampler[5];

		private float scale = 0.5f;
		private int currentSamplerIndex = 0;
		private Color[] colors = new Color[]
		{
			Color.Red,
			Color.Green,
			Color.Blue,
			Color.Yellow,
		};

		private string GetSamplerString(int index)
		{
			switch (index)
			{
				case 0:
					return "PointClamp";
				case 1:
					return "LinearClamp";
				case 2:
					return "PointClamp with Mip LOD Bias = 0.25";
				case 3:
					return "PointClamp with Min LOD = 1";
				case 4:
					return "PointClamp with Max LOD = 1";
				default:
					throw new System.Exception("Unknown sampler!");
			}
		}

		public RenderTextureMipmapsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Left and Right to shrink/expand the scale of the quad");
			Logger.LogInfo("Press Down to cycle through sampler states");
			Logger.LogInfo(GetSamplerString(currentSamplerIndex));

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadWithMatrix.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create<TransformVertexUniform>(vertShaderModule, "main", 0);
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create samplers
			SamplerCreateInfo samplerCreateInfo = SamplerCreateInfo.PointClamp;
			samplers[0] = new Sampler(GraphicsDevice, samplerCreateInfo);

			samplerCreateInfo = SamplerCreateInfo.LinearClamp;
			samplers[1] = new Sampler(GraphicsDevice, samplerCreateInfo);

			samplerCreateInfo = SamplerCreateInfo.PointClamp;
			samplerCreateInfo.MipLodBias = 0.25f;
			samplers[2] = new Sampler(GraphicsDevice, samplerCreateInfo);

			samplerCreateInfo = SamplerCreateInfo.PointClamp;
			samplerCreateInfo.MinLod = 1;
			samplers[3] = new Sampler(GraphicsDevice, samplerCreateInfo);

			samplerCreateInfo = SamplerCreateInfo.PointClamp;
			samplerCreateInfo.MaxLod = 1;
			samplers[4] = new Sampler(GraphicsDevice, samplerCreateInfo);

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

			texture = Texture.CreateTexture2D(
				GraphicsDevice,
				MainWindow.Width,
				MainWindow.Height,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
				4
			);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			// Clear each mip level to a different color
			for (uint i = 0; i < texture.LevelCount; i += 1)
			{
				ColorAttachmentInfo attachmentInfo = new ColorAttachmentInfo
				{
					TextureSlice = new TextureSlice
					{
						Texture = texture,
						Layer = 0,
						MipLevel = i
					},
					ClearColor = colors[i],
					LoadOp = LoadOp.Clear,
					StoreOp = StoreOp.Store,
					WriteOption = WriteOptions.SafeOverwrite
				};
				cmdbuf.BeginRenderPass(attachmentInfo);
				cmdbuf.EndRenderPass();
			}

			GraphicsDevice.Submit(cmdbuf);
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Left))
			{
				scale = System.MathF.Max(0.01f, scale - 0.01f);
			}

			if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Right))
			{
				scale = System.MathF.Min(1f, scale + 0.01f);
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
			{
				currentSamplerIndex = (currentSamplerIndex + 1) % samplers.Length;
				Logger.LogInfo(GetSamplerString(currentSamplerIndex));
			}
		}

		protected override void Draw(double alpha)
		{
			TransformVertexUniform vertUniforms = new TransformVertexUniform(Matrix4x4.CreateScale(scale, scale, 1));

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, samplers[currentSamplerIndex]));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			RenderTextureMipmapsGame game = new RenderTextureMipmapsGame();
			game.Run();
		}
	}
}
