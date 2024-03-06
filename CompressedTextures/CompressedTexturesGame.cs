using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.IO;

namespace MoonWorks.Test
{
	class CompressedTexturesGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Sampler sampler;
		private Texture[] textures;
		private string[] textureNames = new string[]
		{
			"BC1",
			"BC2",
			"BC3",
			"BC7"
		};

		private int currentTextureIndex;

		public CompressedTexturesGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.Backend, 60, true)
		{
			Logger.LogInfo("Press Left and Right to cycle between textures");
			Logger.LogInfo("Setting texture to: " + textureNames[0]);

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
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create sampler
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);

			// Create texture array
			textures = new Texture[textureNames.Length];

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1))
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

			for (int i = 0; i < textureNames.Length; i += 1)
			{
				Logger.LogInfo(textureNames[i]);
				textures[i] = resourceUploader.CreateTextureFromDDS(TestUtils.GetTexturePath(textureNames[i] + ".dds"));
			}

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta)
		{
			int prevSamplerIndex = currentTextureIndex;

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				currentTextureIndex -= 1;
				if (currentTextureIndex < 0)
				{
					currentTextureIndex = textureNames.Length - 1;
				}
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
			{
				currentTextureIndex += 1;
				if (currentTextureIndex >= textureNames.Length)
				{
					currentTextureIndex = 0;
				}
			}

			if (prevSamplerIndex != currentTextureIndex)
			{
				Logger.LogInfo("Setting texture to: " + textureNames[currentTextureIndex]);
			}
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(textures[currentTextureIndex], sampler));
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			CompressedTexturesGame game = new CompressedTexturesGame();
			game.Run();
		}
	}
}
