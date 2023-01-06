using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class TexturedQuadGame : Game
	{
		private GraphicsPipeline pipeline;
		private Buffer vertexBuffer;
		private Buffer indexBuffer;
		private Texture texture;
		private Sampler[] samplers = new Sampler[6];
		private string[] samplerNames = new string[]
		{
			"PointClamp",
			"PointWrap",
			"LinearClamp",
			"LinearWrap",
			"AnisotropicClamp",
			"AnisotropicWrap"
		};

		private int currentSamplerIndex;

		public TexturedQuadGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Left and Right to cycle between sampler states");
			Logger.LogInfo("Setting sampler state to: " + samplerNames[0]);

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadVert.spv"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadFrag.spv"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create samplers
			samplers[0] = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);
			samplers[1] = new Sampler(GraphicsDevice, SamplerCreateInfo.PointWrap);
			samplers[2] = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearClamp);
			samplers[3] = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);
			samplers[4] = new Sampler(GraphicsDevice, SamplerCreateInfo.AnisotropicClamp);
			samplers[5] = new Sampler(GraphicsDevice, SamplerCreateInfo.AnisotropicWrap);

			// Create and populate the GPU resources
			vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
			indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				vertexBuffer,
				new PositionTextureVertex[]
				{
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(4, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(4, 4)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 4)),
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
			texture = Texture.LoadPNG(GraphicsDevice, cmdbuf, TestUtils.GetTexturePath("ravioli.png"));
			GraphicsDevice.Submit(cmdbuf);
			GraphicsDevice.Wait();
		}

		protected override void Update(System.TimeSpan delta)
		{
			int prevSamplerIndex = currentSamplerIndex;

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				currentSamplerIndex -= 1;
				if (currentSamplerIndex < 0)
				{
					currentSamplerIndex = samplers.Length - 1;
				}
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
			{
				currentSamplerIndex += 1;
				if (currentSamplerIndex >= samplers.Length)
				{
					currentSamplerIndex = 0;
				}
			}

			if (prevSamplerIndex != currentSamplerIndex)
			{
				Logger.LogInfo("Setting sampler state to: " + samplerNames[currentSamplerIndex]);
			}
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, samplers[currentSamplerIndex]));
				cmdbuf.DrawIndexedPrimitives(0, 0, 2, 0, 0);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			TexturedQuadGame game = new TexturedQuadGame();
			game.Run();
		}
	}
}
