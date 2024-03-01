using System;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class RenderTexture2DArrayGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture rt;
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

		public RenderTexture2DArrayGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad2DArray.frag"));

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
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointWrap);

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

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			// Clear each depth slice of the RT to a different color
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

			GraphicsDevice.Submit(cmdbuf);
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			t += 0.01f;
			t %= 3;
			FragUniform fragUniform = new FragUniform(MathF.Floor(t));

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(rt, sampler));
				cmdbuf.PushFragmentShaderUniforms(fragUniform);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			RenderTexture2DArrayGame game = new RenderTexture2DArrayGame();
			game.Run();
		}
	}
}
