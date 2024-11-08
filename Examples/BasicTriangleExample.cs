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

		Shader vertShaderModule = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("RawTriangle.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader fragShaderModule = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Fragment
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShaderModule,
			fragShaderModule
		);
		FillPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState.FillMode = FillMode.Line;
		LinePipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
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
				new ColorTargetInfo(swapchainTexture, Color.Black)
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

			renderPass.DrawPrimitives(3, 1, 0, 0);

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
