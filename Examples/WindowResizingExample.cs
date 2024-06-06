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

		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			ShaderStage.Vertex,
			ShaderFormat.SPIRV
		);

		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			ShaderStage.Fragment,
			ShaderFormat.SPIRV
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
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
		}
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(
					swapchainTexture,
					false,
					Color.Black
				)
			);
			renderPass.BindGraphicsPipeline(pipeline);
			renderPass.DrawPrimitives(0, 1);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		pipeline.Dispose();
		Window.SetSize(640, 480);
    }
}
