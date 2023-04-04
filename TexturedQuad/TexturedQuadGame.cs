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

		private Texture[] textures = new Texture[4];
		private string[] imageLoadFormatNames = new string[]
		{
			"PNGFromFile",
			"PNGFromMemory",
			"QOIFromFile",
			"QOIFromMemory"
		};

		private int currentTextureIndex;

		private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

		public TexturedQuadGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Left and Right to cycle between sampler states");
			Logger.LogInfo("Setting sampler state to: " + samplerNames[0]);

			Logger.LogInfo("Press Down to cycle between image load formats");
			Logger.LogInfo("Setting image format to: " + imageLoadFormatNames[0]);

			var pngBytes = System.IO.File.ReadAllBytes(TestUtils.GetTexturePath("ravioli.png"));
			var qoiBytes = System.IO.File.ReadAllBytes(TestUtils.GetTexturePath("ravioli.qoi"));

			Logger.LogInfo(pngBytes.Length.ToString());
			Logger.LogInfo(qoiBytes.Length.ToString());

			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadVert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadFrag"));

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
			textures[0] = Texture.LoadPNG(GraphicsDevice, cmdbuf, TestUtils.GetTexturePath("ravioli.png"));
			textures[1] = Texture.LoadPNG(GraphicsDevice, cmdbuf, pngBytes);
			textures[2] = Texture.LoadQOI(GraphicsDevice, cmdbuf, TestUtils.GetTexturePath("ravioli.qoi"));
			textures[3] = Texture.LoadQOI(GraphicsDevice, cmdbuf, qoiBytes);
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

			int prevTextureIndex = currentTextureIndex;

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
			{
				currentTextureIndex = (currentTextureIndex + 1) % imageLoadFormatNames.Length;
			}

			if (prevTextureIndex != currentTextureIndex)
			{
				Logger.LogInfo("Setting texture format to: " + imageLoadFormatNames[currentTextureIndex]);
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
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(textures[currentTextureIndex], samplers[currentSamplerIndex]));
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
