using MoonWorks;
using System.Numerics;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class DepthMSAAExample : Example
{
	private GraphicsPipeline[] CubePipelines = new GraphicsPipeline[4];
	private Texture[] RenderTargets = new Texture[4];
	private Texture[] DepthRTs = new Texture[4];
	private Texture ResolveTarget;
	private Buffer CubeVertexBuffer1;
	private Buffer CubeVertexBuffer2;
	private Buffer CubeIndexBuffer;

	private float cubeTimer;
	private Quaternion cubeRotation;
	private Quaternion previousCubeRotation;
	private Vector3 camPos;
	private SampleCount currentSampleCount;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("DepthMSAA");

		cubeTimer = 0;
		cubeRotation = Quaternion.Identity;
		previousCubeRotation = Quaternion.Identity;
		camPos = new Vector3(0, 1.5f, 4);
		currentSampleCount = SampleCount.Four;

		Logger.LogInfo("Press Left and Right to cycle between sample counts");
		Logger.LogInfo("Setting sample count to: " + currentSampleCount);

		// Create the cube pipelines
		Shader cubeVertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("PositionColorWithMatrix.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader cubeFragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Fragment
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = new GraphicsPipelineCreateInfo
		{
			TargetInfo = new GraphicsPipelineTargetInfo
			{
				ColorTargetDescriptions = [
					new ColorTargetDescription
					{
						Format = Window.SwapchainFormat,
						BlendState = ColorTargetBlendState.Opaque
					}
				],
				HasDepthStencilTarget = true,
				DepthStencilFormat = GraphicsDevice.SupportedDepthFormat
			},
			DepthStencilState = new DepthStencilState
			{
				EnableDepthTest = true,
				EnableDepthWrite = true,
				CompareOp = CompareOp.LessOrEqual
			},
			VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
			PrimitiveType = PrimitiveType.TriangleList,
			RasterizerState = RasterizerState.CW_CullBack,
			MultisampleState = MultisampleState.None,
			VertexShader = cubeVertShader,
			FragmentShader = cubeFragShader
		};

		for (int i = 0; i < CubePipelines.Length; i += 1)
		{
			pipelineCreateInfo.MultisampleState.SampleCount = (SampleCount) i;
			CubePipelines[i] = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
		}

		// Create the MSAA render textures and depth textures
		for (int i = 0; i < RenderTargets.Length; i += 1)
		{
			RenderTargets[i] = Texture.Create2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				Window.SwapchainFormat,
				TextureUsageFlags.ColorTarget,
				1,
				(SampleCount) i
			);

			DepthRTs[i] = Texture.Create2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				GraphicsDevice.SupportedDepthFormat,
				TextureUsageFlags.DepthStencilTarget,
				1,
				(SampleCount) i
			);
		}

		ResolveTarget = Texture.Create2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			Window.SwapchainFormat,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		// Create the buffers
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		var cubeVertexData = new System.Span<PositionColorVertex>(
		[
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
		]);

		CubeVertexBuffer1 = resourceUploader.CreateBuffer(
			cubeVertexData,
			BufferUsageFlags.Vertex
		);

		// Scoot all the verts slightly for the second cube...
		for (int i = 0; i < cubeVertexData.Length; i += 1)
		{
			cubeVertexData[i].Position.Z += 3;
		}

		CubeVertexBuffer2 = resourceUploader.CreateBuffer(
			cubeVertexData,
			BufferUsageFlags.Vertex
		);

		CubeIndexBuffer = resourceUploader.CreateBuffer<uint>(
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

	public override void Update(System.TimeSpan delta)
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

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			// Set up cube model-view-projection matrix
			Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
				float.DegreesToRadians(75f),
				(float) Window.Width / Window.Height,
				0.01f,
				100f
			);
			Matrix4x4 view = Matrix4x4.CreateLookAt(camPos, Vector3.Zero, Vector3.UnitY);
			Matrix4x4 model = Matrix4x4.CreateFromQuaternion(
				Quaternion.Slerp(
					previousCubeRotation,
					cubeRotation,
					(float) alpha
				)
			);
			TransformVertexUniform cubeUniforms = new TransformVertexUniform(model * view * proj);

			int index = (int) currentSampleCount;

			ColorTargetInfo colorTargetInfo;

			if (currentSampleCount == SampleCount.One)
			{
				colorTargetInfo = new ColorTargetInfo(swapchainTexture, Color.Black);
			}
			else
			{
				colorTargetInfo = new ColorTargetInfo
				{
					Texture = RenderTargets[index].Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black,
					StoreOp = StoreOp.Resolve,
					ResolveTexture = swapchainTexture.Handle
				};
			}

			// Begin the MSAA RT pass
			var renderPass = cmdbuf.BeginRenderPass(
				new DepthStencilTargetInfo(DepthRTs[index], 1, true),
				colorTargetInfo
			);
			renderPass.BindGraphicsPipeline(CubePipelines[index]);

			cmdbuf.PushVertexUniformData(cubeUniforms);

			// Draw the first cube
			renderPass.BindVertexBuffers(CubeVertexBuffer1);
			renderPass.BindIndexBuffer(CubeIndexBuffer, IndexElementSize.ThirtyTwo);
			renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);

			// Draw the second cube
			renderPass.BindVertexBuffers(CubeVertexBuffer2);
			renderPass.BindIndexBuffer(CubeIndexBuffer, IndexElementSize.ThirtyTwo);
			renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);

			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        for (var i = 0; i < 4; i += 1)
		{
			CubePipelines[i].Dispose();
			RenderTargets[i].Dispose();
			DepthRTs[i].Dispose();
		}
		ResolveTarget.Dispose();
		CubeVertexBuffer1.Dispose();
		CubeVertexBuffer2.Dispose();
		CubeIndexBuffer.Dispose();
    }
}
