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
		Shader triangleVertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("RawTriangle.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				HLSLShaderModel = ShaderCross.HLSLShaderModel.Six,
				Stage = ShaderStage.Vertex
			}
		);

		Shader triangleFragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				HLSLShaderModel = ShaderCross.HLSLShaderModel.Six,
				Stage = ShaderStage.Fragment
			}
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			triangleVertShader,
			triangleFragShader
		);
		for (int i = 0; i < MsaaPipelines.Length; i += 1)
		{
			pipelineCreateInfo.MultisampleState.SampleCount = (SampleCount) i;
			MsaaPipelines[i] = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
		}

		// Create the MSAA render textures
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
		}

		// Create the sampler
		RTSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);
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

			ColorTargetInfo colorTargetInfo;

			if (currentSampleCount == SampleCount.One)
			{
				colorTargetInfo = new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black
				};
			}
			else
			{
				colorTargetInfo = new ColorTargetInfo
				{
					Texture = rt.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black,
					Cycle = true,
					CycleResolveTexture = true,
					ResolveTexture = swapchainTexture.Handle,
					StoreOp = StoreOp.Resolve
				};
			}

			var renderPass = cmdbuf.BeginRenderPass(colorTargetInfo);
			renderPass.BindGraphicsPipeline(MsaaPipelines[(int) currentSampleCount]);
			renderPass.DrawPrimitives(3, 1, 0, 0);
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

		RTSampler.Dispose();
    }
}
