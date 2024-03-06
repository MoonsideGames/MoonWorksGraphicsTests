using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TextureMipmapsGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture texture;
		private Sampler sampler;

		private float scale = 0.5f;

		public TextureMipmapsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.DefaultBackend, 60, true)
		{
			Logger.LogInfo("Press Left and Right to shrink/expand the scale of the quad");

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

			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create and populate the GPU resources
			texture = Texture.CreateTexture2D(
				GraphicsDevice,
				256,
				256,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.Sampler,
				4
			);

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
					0, 2, 3
				],
				BufferUsageFlags.Index
			);

			// Set the various mip levels
			for (uint i = 0; i < texture.LevelCount; i += 1)
			{
				var w = texture.Width >> (int) i;
				var h = texture.Height >> (int) i;
				var region = new TextureRegion
				{
					TextureSlice = new TextureSlice
					{
						Texture = texture,
						Layer = 0,
						MipLevel = i
					},
					X = 0,
					Y = 0,
					Z = 0,
					Width = w,
					Height = h,
					Depth = 1
				};

				resourceUploader.SetTextureDataFromCompressed(
					region,
					TestUtils.GetTexturePath($"mip{i}.png")
				);
			}

			resourceUploader.Upload();
			resourceUploader.Dispose();
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
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			TextureMipmapsGame game = new TextureMipmapsGame();
			game.Run();
		}
	}
}
