using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class MSAAExample : Example
{
	private GraphicsPipeline[] MsaaPipelines = new GraphicsPipeline[4];
	private Texture[] RenderTargets = new Texture[4];
	private Sampler RTSampler;

	private SampleCount currentSampleCount;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("MSAA");

		currentSampleCount = SampleCount.Four;

		Logger.LogInfo("Press Left and Right to cycle between sample counts");
		Logger.LogInfo("Setting sample count to: " + currentSampleCount);

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
			pipelineCreateInfo.MultisampleState.MultisampleCount = (SampleCount) i;
			MsaaPipelines[i] = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
		}

		// Create the blit pipeline
		/*
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
		*/

		// Create the MSAA render textures
		for (int i = 0; i < RenderTargets.Length; i += 1)
		{
			RenderTargets[i] = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
				1,
				(SampleCount) i
			);
		}

		// Create the sampler
		RTSampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);
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
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			Texture rt = RenderTargets[(int) currentSampleCount];

			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(
					rt,
					true,
					Color.Black
				)
			);
			renderPass.BindGraphicsPipeline(MsaaPipelines[(int) currentSampleCount]);
			renderPass.DrawPrimitives(0, 1);
			cmdbuf.EndRenderPass(renderPass);

			cmdbuf.Blit(rt, swapchainTexture, Filter.Nearest, false);
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

		RTSampler.Dispose();
    }
}
