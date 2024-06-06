using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class BasicTriangleExample : Example
{
	private GraphicsPipeline FillPipeline;
	private GraphicsPipeline LinePipeline;

	private Viewport SmallViewport = new Viewport(160, 120, 320, 240);
	private Rect ScissorRect = new Rect(320, 240, 320, 240);

	private bool UseWireframeMode;
	private bool UseSmallViewport;
	private bool UseScissorRect;

	public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
	{
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("BasicTriangle");

		Logger.LogInfo("Press Left to toggle wireframe mode\nPress Down to toggle small viewport\nPress Right to toggle scissor rect");

		Shader vertShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			ShaderStage.Vertex,
			ShaderFormat.SPIRV
		);

		Shader fragShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			ShaderStage.Fragment,
			ShaderFormat.SPIRV
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShaderModule,
			fragShaderModule
		);
		FillPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState.FillMode = FillMode.Line;
		LinePipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
	}

	public override void Update(System.TimeSpan delta)
	{
		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			UseWireframeMode = !UseWireframeMode;
			Logger.LogInfo("Using wireframe mode: " + UseWireframeMode);
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
		{
			UseSmallViewport = !UseSmallViewport;
			Logger.LogInfo("Using small viewport: " + UseSmallViewport);
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			UseScissorRect = !UseScissorRect;
			Logger.LogInfo("Using scissor rect: " + UseScissorRect);
		}
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(swapchainTexture, false, Color.Black)
			);

			renderPass.BindGraphicsPipeline(UseWireframeMode ? LinePipeline : FillPipeline);

			if (UseSmallViewport)
			{
				renderPass.SetViewport(SmallViewport);
			}
			if (UseScissorRect)
			{
				renderPass.SetScissor(ScissorRect);
			}

			renderPass.DrawPrimitives(0, 1);

			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Destroy()
	{
		FillPipeline.Dispose();
		LinePipeline.Dispose();
	}
}
