using MoonWorks;
using MoonWorks.Math.Float;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class MSAAGame : Game
	{
		private GraphicsPipeline[] msaaPipelines = new GraphicsPipeline[4];

		private ShaderModule triangleVertShaderModule;
		private ShaderModule triangleFragShaderModule;

		private GraphicsPipeline blitPipeline;
		private ShaderModule blitVertShaderModule;
		private ShaderModule blitFragShaderModule;

		private Texture rt;
		private Sampler rtSampler;
		private Buffer quadVertexBuffer;
		private Buffer quadIndexBuffer;

		private SampleCount currentSampleCount = SampleCount.Four;

		public MSAAGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press A and D to cycle between sample counts");
			Logger.LogInfo("Setting sample count to: " + currentSampleCount);

			// Create the MSAA pipelines
			triangleVertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangleVertices.spv"));
			triangleFragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.spv"));

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				triangleVertShaderModule,
				triangleFragShaderModule
			);
			for (int i = 0; i < msaaPipelines.Length; i += 1)
			{
				pipelineCreateInfo.MultisampleState.MultisampleCount = (SampleCount) i;
				msaaPipelines[i] = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
			}

			// Create the blit pipeline
			blitVertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadVert.spv"));
			blitFragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadFrag.spv"));

			pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				blitVertShaderModule,
				blitFragShaderModule
			);
			pipelineCreateInfo.VertexInputState = new VertexInputState(
				VertexBinding.Create<PositionTextureVertex>(),
				VertexAttribute.Create<PositionTextureVertex>("Position", 0),
				VertexAttribute.Create<PositionTextureVertex>("TexCoord", 1)
			);
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			blitPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create the MSAA render texture and sampler
			rt = Texture.CreateTexture2D(
				GraphicsDevice,
				MainWindow.Width,
				MainWindow.Height,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
			);
			rtSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create and populate the vertex and index buffers
			quadVertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
			quadIndexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(
				quadVertexBuffer,
				new PositionTextureVertex[]
				{
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
				}
			);
			cmdbuf.SetBufferData(
				quadIndexBuffer,
				new ushort[]
				{
					0, 1, 2,
					0, 2, 3,
				}
			);
			GraphicsDevice.Submit(cmdbuf);
			GraphicsDevice.Wait();
		}

		protected override void Update(System.TimeSpan delta)
		{
			SampleCount prevSampleCount = currentSampleCount;

			if (Inputs.Keyboard.IsPressed(Input.KeyCode.A))
			{
				currentSampleCount -= 1;
				if (currentSampleCount < 0)
				{
					currentSampleCount = SampleCount.Eight;
				}
			}
			if (Inputs.Keyboard.IsPressed(Input.KeyCode.D))
			{
				currentSampleCount += 1;
				if (currentSampleCount > SampleCount.Eight)
				{
					currentSampleCount = SampleCount.One;
				}
			}

			if (prevSampleCount != currentSampleCount)
			{
				Logger.LogInfo("Setting sample count to: " + currentSampleCount);
			}
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(
					new ColorAttachmentInfo(
						rt,
						Color.Black,
						currentSampleCount,
						StoreOp.DontCare
					)
				);
				cmdbuf.BindGraphicsPipeline(msaaPipelines[(int) currentSampleCount]);
				cmdbuf.DrawPrimitives(0, 1, 0, 0);
				cmdbuf.EndRenderPass();

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, LoadOp.DontCare));
				cmdbuf.BindGraphicsPipeline(blitPipeline);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(rt, rtSampler));
				cmdbuf.BindVertexBuffers(quadVertexBuffer);
				cmdbuf.BindIndexBuffer(quadIndexBuffer, IndexElementSize.Sixteen);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2, 0, 0);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			MSAAGame game = new MSAAGame();
			game.Run();
		}
	}
}
