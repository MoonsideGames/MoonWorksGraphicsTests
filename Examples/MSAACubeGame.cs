using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MoonWorks.Math;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
	class MSAACubeGame : Game
	{
		private GraphicsPipeline[] msaaPipelines = new GraphicsPipeline[4];
		private GraphicsPipeline cubemapPipeline;

		private Texture[] renderTargets = new Texture[4];
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Sampler sampler;

		private Vector3 camPos = new Vector3(0, 0, 4f);

		private SampleCount currentSampleCount = SampleCount.Four;

		public MSAACubeGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.PreferredBackends, 60, true)
		{
			Logger.LogInfo("Press Down to view the other side of the cubemap");
			Logger.LogInfo("Press Left and Right to cycle between sample counts");
			Logger.LogInfo("Setting sample count to: " + currentSampleCount);

			// Create the MSAA pipelines
			ShaderModule triangleVertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangle.vert"));
			ShaderModule triangleFragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				TextureFormat.R8G8B8A8,
				triangleVertShaderModule,
				triangleFragShaderModule
			);
			for (int i = 0; i < msaaPipelines.Length; i += 1)
			{
				pipelineCreateInfo.MultisampleState.MultisampleCount = (SampleCount)i;
				msaaPipelines[i] = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
			}

			// Create the cubemap pipeline
			ShaderModule cubemapVertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("Skybox.vert"));
			ShaderModule cubemapFragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("Skybox.frag"));

			pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				cubemapVertShaderModule,
				cubemapFragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>();
			pipelineCreateInfo.VertexShaderInfo.UniformBufferSize = (uint)Marshal.SizeOf<TransformVertexUniform>();
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			cubemapPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create the MSAA render targets
			for (int i = 0; i < renderTargets.Length; i++)
			{
				TextureCreateInfo cubeCreateInfo = new TextureCreateInfo
				{
					Width = 16,
					Height = 16,
					Format = TextureFormat.R8G8B8A8,
					Depth = 1,
					LevelCount = 1,
					SampleCount = (SampleCount)i,
					UsageFlags = TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
					IsCube = true,
					LayerCount = 6
				};
				renderTargets[i] = new Texture(GraphicsDevice, cubeCreateInfo);
			}

			// Create samplers
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionVertex(new Vector3(-10, -10, -10)),
					new PositionVertex(new Vector3(10, -10, -10)),
					new PositionVertex(new Vector3(10, 10, -10)),
					new PositionVertex(new Vector3(-10, 10, -10)),

					new PositionVertex(new Vector3(-10, -10, 10)),
					new PositionVertex(new Vector3(10, -10, 10)),
					new PositionVertex(new Vector3(10, 10, 10)),
					new PositionVertex(new Vector3(-10, 10, 10)),

					new PositionVertex(new Vector3(-10, -10, -10)),
					new PositionVertex(new Vector3(-10, 10, -10)),
					new PositionVertex(new Vector3(-10, 10, 10)),
					new PositionVertex(new Vector3(-10, -10, 10)),

					new PositionVertex(new Vector3(10, -10, -10)),
					new PositionVertex(new Vector3(10, 10, -10)),
					new PositionVertex(new Vector3(10, 10, 10)),
					new PositionVertex(new Vector3(10, -10, 10)),

					new PositionVertex(new Vector3(-10, -10, -10)),
					new PositionVertex(new Vector3(-10, -10, 10)),
					new PositionVertex(new Vector3(10, -10, 10)),
					new PositionVertex(new Vector3(10, -10, -10)),

					new PositionVertex(new Vector3(-10, 10, -10)),
					new PositionVertex(new Vector3(-10, 10, 10)),
					new PositionVertex(new Vector3(10, 10, 10)),
					new PositionVertex(new Vector3(10, 10, -10))
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0,  1,  2,  0,  2,  3,
					6,  5,  4,  7,  6,  4,
					8,  9, 10,  8, 10, 11,
					14, 13, 12, 15, 14, 12,
					16, 17, 18, 16, 18, 19,
					22, 21, 20, 23, 22, 20
				],
				BufferUsageFlags.Index
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();
		}

		protected override void Update(System.TimeSpan delta)
		{
			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
			{
				camPos.Z *= -1;
			}

			SampleCount prevSampleCount = currentSampleCount;

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				currentSampleCount -= 1;
				if (currentSampleCount < 0)
				{
					currentSampleCount = SampleCount.Eight;
				}
			}
			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
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
			Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(75f),
				(float)MainWindow.Width / MainWindow.Height,
				0.01f,
				100f
			);
			Matrix4x4 view = Matrix4x4.CreateLookAt(
				camPos,
				Vector3.Zero,
				Vector3.Up
			);
			TransformVertexUniform vertUniforms = new TransformVertexUniform(view * proj);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				// Get a reference to the RT for the given sample count
				int rtIndex = (int) currentSampleCount;
				Texture rt = renderTargets[rtIndex];
				ColorAttachmentInfo rtAttachmentInfo = new ColorAttachmentInfo(
					rt,
					WriteOptions.Cycle,
					Color.Black
				);

				// Render a triangle to each slice of the cubemap
				for (uint i = 0; i < 6; i += 1)
				{
					rtAttachmentInfo.TextureSlice.Layer = i;

					cmdbuf.BeginRenderPass(rtAttachmentInfo);
					cmdbuf.BindGraphicsPipeline(msaaPipelines[rtIndex]);
					cmdbuf.DrawPrimitives(0, 1);
					cmdbuf.EndRenderPass();
				}

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.Cycle, Color.Black));
				cmdbuf.BindGraphicsPipeline(cubemapPipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(rt, sampler));
				cmdbuf.PushVertexShaderUniforms(vertUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 12);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			MSAACubeGame game = new MSAACubeGame();
			game.Run();
		}
	}
}
