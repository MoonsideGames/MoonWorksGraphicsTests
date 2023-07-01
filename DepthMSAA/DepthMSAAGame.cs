using MoonWorks;
using MoonWorks.Math.Float;
using MoonWorks.Math;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class DepthMSAAGame : Game
	{
		private GraphicsPipeline[] cubePipelines = new GraphicsPipeline[4];
		private GraphicsPipeline blitPipeline;
		private Texture[] renderTargets = new Texture[4];
		private Texture[] depthRTs = new Texture[4];
		private Sampler rtSampler;
		private Buffer cubeVertexBuffer1;
		private Buffer cubeVertexBuffer2;
		private Buffer cubeIndexBuffer;
		private Buffer quadVertexBuffer;
		private Buffer quadIndexBuffer;

		private float cubeTimer = 0f;
		private Quaternion cubeRotation = Quaternion.Identity;
		private Quaternion previousCubeRotation = Quaternion.Identity;
		private Vector3 camPos = new Vector3(0, 1.5f, 4f);

		private SampleCount currentSampleCount = SampleCount.Four;

		public DepthMSAAGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Logger.LogInfo("Press Left and Right to cycle between sample counts");
			Logger.LogInfo("Setting sample count to: " + currentSampleCount);

			// Create the cube pipelines
			ShaderModule cubeVertShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("PositionColorWithMatrix.vert")
			);
			ShaderModule cubeFragShaderModule = new ShaderModule(
				GraphicsDevice,
				TestUtils.GetShaderPath("SolidColor.frag")
			);

			GraphicsPipelineCreateInfo pipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					TextureFormat.D32,
					new ColorAttachmentDescription(
						MainWindow.SwapchainFormat,
						ColorAttachmentBlendState.Opaque
					)
				),
				DepthStencilState = DepthStencilState.DepthReadWrite,
				VertexShaderInfo = GraphicsShaderInfo.Create<TransformVertexUniform>(cubeVertShaderModule, "main", 0),
				VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				FragmentShaderInfo = GraphicsShaderInfo.Create(cubeFragShaderModule, "main", 0),
				RasterizerState = RasterizerState.CW_CullBack,
				MultisampleState = MultisampleState.None
			};

			for (int i = 0; i < cubePipelines.Length; i += 1)
			{
				pipelineCreateInfo.MultisampleState.MultisampleCount = (SampleCount) i;
				cubePipelines[i] = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
			}

			// Create the blit pipeline
			ShaderModule blitVertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule blitFragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.frag"));

			pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				blitVertShaderModule,
				blitFragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			blitPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create the MSAA render textures and depth textures
			for (int i = 0; i < renderTargets.Length; i += 1)
			{
				renderTargets[i] = Texture.CreateTexture2D(
					GraphicsDevice,
					MainWindow.Width,
					MainWindow.Height,
					TextureFormat.R8G8B8A8,
					TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
					1,
					(SampleCount) i
				);

				depthRTs[i] = Texture.CreateTexture2D(
					GraphicsDevice,
					MainWindow.Width,
					MainWindow.Height,
					TextureFormat.D32,
					TextureUsageFlags.DepthStencilTarget,
					1,
					(SampleCount) i
				);
			}

			// Create the sampler
			rtSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create the buffers
			quadVertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
			quadIndexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

			cubeVertexBuffer1 = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 24);
			cubeVertexBuffer2 = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 24);
			cubeIndexBuffer = Buffer.Create<uint>(GraphicsDevice, BufferUsageFlags.Index, 36);

			// Populate the GPU resources
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

			PositionColorVertex[] vertices = new PositionColorVertex[]
			{
				new PositionColorVertex(new Vector3(-1, -1, -1), new Color(1f, 0f, 0f)),
				new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, -1), new Color(1f, 0f, 0f)),
				new PositionColorVertex(new Vector3(-1, 1, -1), new Color(1f, 0f, 0f)),

				new PositionColorVertex(new Vector3(-1, -1, 1), new Color(0f, 1f, 0f)),
				new PositionColorVertex(new Vector3(1, -1, 1), new Color(0f, 1f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, 1), new Color(0f, 1f, 0f)),
				new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 1f, 0f)),

				new PositionColorVertex(new Vector3(-1, -1, -1), new Color(0f, 0f, 1f)),
				new PositionColorVertex(new Vector3(-1, 1, -1), new Color(0f, 0f, 1f)),
				new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 0f, 1f)),
				new PositionColorVertex(new Vector3(-1, -1, 1), new Color(0f, 0f, 1f)),

				new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, -1), new Color(1f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, 1), new Color(1f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(1, -1, 1), new Color(1f, 0.5f, 0f)),

				new PositionColorVertex(new Vector3(-1, -1, -1), new Color(1f, 0f, 0.5f)),
				new PositionColorVertex(new Vector3(-1, -1, 1), new Color(1f, 0f, 0.5f)),
				new PositionColorVertex(new Vector3(1, -1, 1), new Color(1f, 0f, 0.5f)),
				new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0f, 0.5f)),

				new PositionColorVertex(new Vector3(-1, 1, -1), new Color(0f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, 1), new Color(0f, 0.5f, 0f)),
				new PositionColorVertex(new Vector3(1, 1, -1), new Color(0f, 0.5f, 0f))
			};
			cmdbuf.SetBufferData(
				cubeVertexBuffer1,
				vertices
			);

			// Scoot all the verts slightly for the second cube...
			for (int i = 0; i < vertices.Length; i += 1)
			{
				vertices[i].Position.Z += 3;
			}
			cmdbuf.SetBufferData(
				cubeVertexBuffer2,
				vertices
			);

			cmdbuf.SetBufferData(
				cubeIndexBuffer,
				new uint[]
				{
					0, 1, 2,	0, 2, 3,
					6, 5, 4,	7, 6, 4,
					8, 9, 10,	8, 10, 11,
					14, 13, 12,	15, 14, 12,
					16, 17, 18,	16, 18, 19,
					22, 21, 20,	23, 22, 20
				}
			);

			GraphicsDevice.Submit(cmdbuf);
		}

		protected override void Update(System.TimeSpan delta)
		{
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

			// Rotate the cube
			cubeTimer += (float) delta.TotalSeconds;
			previousCubeRotation = cubeRotation;
			cubeRotation = Quaternion.CreateFromYawPitchRoll(cubeTimer * 2f, 0, cubeTimer * 2f);
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				// Set up cube model-view-projection matrix
				Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
					MathHelper.ToRadians(75f),
					(float) MainWindow.Width / MainWindow.Height,
					0.01f,
					100f
				);
				Matrix4x4 view = Matrix4x4.CreateLookAt(camPos, Vector3.Zero, Vector3.Up);
				Matrix4x4 model = Matrix4x4.CreateFromQuaternion(
					Quaternion.Slerp(
						previousCubeRotation,
						cubeRotation,
						(float) alpha
					)
				);
				TransformVertexUniform cubeUniforms = new TransformVertexUniform(model * view * proj);

				// Begin the MSAA RT pass
				int index = (int) currentSampleCount;
				cmdbuf.BeginRenderPass(
					new DepthStencilAttachmentInfo(depthRTs[index], new DepthStencilValue(1, 0)),
					new ColorAttachmentInfo(renderTargets[index], Color.Black)
				);
				cmdbuf.BindGraphicsPipeline(cubePipelines[index]);

				// Draw the first cube
				cmdbuf.BindVertexBuffers(cubeVertexBuffer1);
				cmdbuf.BindIndexBuffer(cubeIndexBuffer, IndexElementSize.ThirtyTwo);
				uint vertexParamOffset = cmdbuf.PushVertexShaderUniforms(cubeUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 12, vertexParamOffset, 0);

				// Draw the second cube
				cmdbuf.BindVertexBuffers(cubeVertexBuffer2);
				cmdbuf.BindIndexBuffer(cubeIndexBuffer, IndexElementSize.ThirtyTwo);
				vertexParamOffset = cmdbuf.PushVertexShaderUniforms(cubeUniforms);
				cmdbuf.DrawIndexedPrimitives(0, 0, 12, vertexParamOffset, 0);

				cmdbuf.EndRenderPass();

				// Blit the MSAA RT to the backbuffer
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, LoadOp.DontCare));
				cmdbuf.BindGraphicsPipeline(blitPipeline);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(renderTargets[index], rtSampler));
				cmdbuf.BindVertexBuffers(quadVertexBuffer);
				cmdbuf.BindIndexBuffer(quadIndexBuffer, IndexElementSize.Sixteen);
				cmdbuf.DrawIndexedPrimitives(0, 0, 2, 0, 0);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			DepthMSAAGame game = new DepthMSAAGame();
			game.Run();
		}
	}
}
