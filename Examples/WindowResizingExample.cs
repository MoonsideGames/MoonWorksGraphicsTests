using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class WindowResizingExample : Example
{
	private GraphicsPipeline pipeline;

	private int currentResolutionIndex;
	private record struct Res(uint Width, uint Height);
	private Res[] resolutions =
    [
        new Res(640, 480),
		new Res(1280, 720),
		new Res(1024, 1024),
		new Res(1600, 900),
		new Res(1920, 1080),
		new Res(3200, 1800),
		new Res(3840, 2160),
	];

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("WindowResizing");

		Logger.LogInfo("Press left and right to resize the window!");

		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.SPIRV,
				Stage = ShaderStage.Vertex
			}
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.SPIRV,
				Stage = ShaderStage.Fragment
			}
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
	}

	public override void Update(System.TimeSpan delta)
	{
		int prevResolutionIndex = currentResolutionIndex;

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			currentResolutionIndex -= 1;
			if (currentResolutionIndex < 0)
			{
				currentResolutionIndex = resolutions.Length - 1;
			}
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			currentResolutionIndex += 1;
			if (currentResolutionIndex >= resolutions.Length)
			{
				currentResolutionIndex = 0;
			}
		}

		if (prevResolutionIndex != currentResolutionIndex)
		{
			Logger.LogInfo("Setting resolution to: " + resolutions[currentResolutionIndex]);
			Window.SetSize(resolutions[currentResolutionIndex].Width, resolutions[currentResolutionIndex].Height);
			Window.SetPositionCentered();
		}
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(pipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		pipeline.Dispose();
    }
}
