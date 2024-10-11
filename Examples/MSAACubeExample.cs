using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using MoonWorks.Math;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class MSAACubeExample : Example
{
	private GraphicsPipeline[] MsaaPipelines = new GraphicsPipeline[4];
	private GraphicsPipeline CubemapPipeline;

	private Texture[] RenderTargets = new Texture[4];
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Sampler Sampler;

	private Vector3 camPos;
	private SampleCount currentSampleCount;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("MSAACube");

		Logger.LogInfo("Press Down to view the other side of the cubemap");
		Logger.LogInfo("Press Left and Right to cycle between sample counts");
		Logger.LogInfo("Setting sample count to: " + currentSampleCount);

		camPos = new Vector3(0, 0, 4);
		currentSampleCount = SampleCount.Four;

		// Create the MSAA pipelines
		Shader triangleVertShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV
			}
		);

		Shader triangleFragShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV
			}
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			TextureFormat.R8G8B8A8Unorm,
			triangleVertShader,
			triangleFragShader
		);
		for (int i = 0; i < MsaaPipelines.Length; i += 1)
		{
			pipelineCreateInfo.MultisampleState.SampleCount = (SampleCount)i;
			MsaaPipelines[i] = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
		}

		// Create the cubemap pipeline
		Shader cubemapVertShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("Skybox.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV,
				NumUniformBuffers = 1
			}
		);

		Shader cubemapFragShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("Skybox.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV,
				NumSamplers = 1
			}
		);

		pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			cubemapVertShader,
			cubemapFragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>();

		CubemapPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create the MSAA render targets
		for (int i = 0; i < RenderTargets.Length; i++)
		{
			RenderTargets[i] = Texture.CreateCube(
				GraphicsDevice,
				16,
				TextureFormat.R8G8B8A8Unorm,
				TextureUsageFlags.ColorTarget
			);
		}

		// Create samplers
		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
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

		IndexBuffer = resourceUploader.CreateBuffer<ushort>(
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

	public override void Draw(double alpha)
	{
		Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
			MathHelper.ToRadians(75f),
			(float)Window.Width / Window.Height,
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
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			// Get a reference to the RT for the given sample count
			int rtIndex = (int) currentSampleCount;
			Texture rt = RenderTargets[rtIndex];
			var rtAttachmentInfo = new ColorTargetInfo
			{
				Texture = rt.Handle,
				LoadOp = LoadOp.Clear,
				ClearColor = Color.Black,
				Cycle = true
			};

			RenderPass renderPass;

			// Render a triangle to each slice of the cubemap
			for (uint i = 0; i < 6; i += 1)
			{
				rtAttachmentInfo.LayerOrDepthPlane = i;

				renderPass = cmdbuf.BeginRenderPass(rtAttachmentInfo);
				renderPass.BindGraphicsPipeline(MsaaPipelines[rtIndex]);
				renderPass.DrawPrimitives(3, 1, 0, 0);
				cmdbuf.EndRenderPass(renderPass);
			}

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black
				}
			);
			renderPass.BindGraphicsPipeline(CubemapPipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(rt, Sampler));
			cmdbuf.PushVertexUniformData(vertUniforms);
			renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        for (var i = 0; i < 4; i += 1)
		{
			MsaaPipelines[i].Dispose();
			RenderTargets[i].Dispose();
		}

		CubemapPipeline.Dispose();
		VertexBuffer.Dispose();
		IndexBuffer.Dispose();
		Sampler.Dispose();
    }
}
