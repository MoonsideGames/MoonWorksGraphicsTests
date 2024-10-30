using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class CullFaceExample : Example
{
	private GraphicsPipeline CW_CullNonePipeline;
	private GraphicsPipeline CW_CullFrontPipeline;
	private GraphicsPipeline CW_CullBackPipeline;
	private GraphicsPipeline CCW_CullNonePipeline;
	private GraphicsPipeline CCW_CullFrontPipeline;
	private GraphicsPipeline CCW_CullBackPipeline;
	private Buffer CW_VertexBuffer;
	private Buffer CCW_VertexBuffer;

	private bool UseClockwiseWinding;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("CullFace");

		Logger.LogInfo("Press Down to toggle the winding order of the triangles (default is counter-clockwise)");

		// Load the shaders
		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("PositionColor.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Vertex
			}
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Fragment
			}
		);

		// Create the graphics pipelines
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();

		pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullNone;
		CW_CullNonePipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullFront;
		CW_CullFrontPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState = RasterizerState.CW_CullBack;
		CW_CullBackPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullNone;
		CCW_CullNonePipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullFront;
		CCW_CullFrontPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		pipelineCreateInfo.RasterizerState = RasterizerState.CCW_CullBack;
		CCW_CullBackPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the vertex buffers
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		CW_VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.Blue),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3(-1, -1, 0), Color.Red),
			],
			BufferUsageFlags.Vertex
		);

		CCW_VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionColorVertex(new Vector3(-1, -1, 0), Color.Red),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.Blue)
			],
			BufferUsageFlags.Vertex
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
		{
			UseClockwiseWinding = !UseClockwiseWinding;
			Logger.LogInfo("Using clockwise winding: " + UseClockwiseWinding);
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

			// Need to bind a pipeline before binding vertex buffers
			renderPass.BindGraphicsPipeline(CW_CullNonePipeline);
			if (UseClockwiseWinding)
			{
				renderPass.BindVertexBuffer(CW_VertexBuffer);
			}
			else
			{
				renderPass.BindVertexBuffer(CCW_VertexBuffer);
			}

			renderPass.SetViewport(new Viewport(0, 0, 213, 240));
			renderPass.DrawPrimitives(3, 1, 0, 0);

			renderPass.SetViewport(new Viewport(213, 0, 213, 240));
			renderPass.BindGraphicsPipeline(CW_CullFrontPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);

			renderPass.SetViewport(new Viewport(426, 0, 213, 240));
			renderPass.BindGraphicsPipeline(CW_CullBackPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);

			renderPass.SetViewport(new Viewport(0, 240, 213, 240));
			renderPass.BindGraphicsPipeline(CCW_CullNonePipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);

			renderPass.SetViewport(new Viewport(213, 240, 213, 240));
			renderPass.BindGraphicsPipeline(CCW_CullFrontPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);

			renderPass.SetViewport(new Viewport(426, 240, 213, 240));
			renderPass.BindGraphicsPipeline(CCW_CullBackPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);

			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        CW_CullNonePipeline.Dispose();
		CW_CullFrontPipeline.Dispose();
		CW_CullBackPipeline.Dispose();
		CCW_CullNonePipeline.Dispose();
		CCW_CullFrontPipeline.Dispose();
		CCW_CullBackPipeline.Dispose();
		CW_VertexBuffer.Dispose();
		CCW_VertexBuffer.Dispose();
    }
}
