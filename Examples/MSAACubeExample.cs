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
	private GpuBuffer VertexBuffer;
	private GpuBuffer IndexBuffer;
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
		Shader triangleVertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV
			}
		);

		Shader triangleFragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV
			}
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			TextureFormat.R8G8B8A8,
			triangleVertShader,
			triangleFragShader
		);
		for (int i = 0; i < MsaaPipelines.Length; i += 1)
		{
			pipelineCreateInfo.MultisampleState.MultisampleCount = (SampleCount)i;
			MsaaPipelines[i] = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
		}

		// Create the cubemap pipeline
		Shader cubemapVertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("Skybox.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV,
				UniformBufferCount = 1
			}
		);

		Shader cubemapFragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("Skybox.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1
			}
		);

		pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			cubemapVertShader,
			cubemapFragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>();

		CubemapPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		// Create the MSAA render targets
		for (int i = 0; i < RenderTargets.Length; i++)
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
			RenderTargets[i] = new Texture(GraphicsDevice, cubeCreateInfo);
		}

		// Create samplers
		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

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
			ColorAttachmentInfo rtAttachmentInfo = new ColorAttachmentInfo(
				rt,
				true,
				Color.Black
			);

			RenderPass renderPass;

			// Render a triangle to each slice of the cubemap
			for (uint i = 0; i < 6; i += 1)
			{
				rtAttachmentInfo.TextureSlice.Layer = i;

				renderPass = cmdbuf.BeginRenderPass(rtAttachmentInfo);
				renderPass.BindGraphicsPipeline(MsaaPipelines[rtIndex]);
				renderPass.DrawPrimitives(0, 1);
				cmdbuf.EndRenderPass(renderPass);
			}

			renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(
					swapchainTexture,
					false,
					Color.Black
				)
			);
			renderPass.BindGraphicsPipeline(CubemapPipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(rt, Sampler));
			renderPass.PushVertexUniformData(vertUniforms);
			renderPass.DrawIndexedPrimitives(0, 0, 12);
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
