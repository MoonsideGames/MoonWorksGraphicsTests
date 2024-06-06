using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class TriangleVertexBufferExample : Example
{
	private GraphicsPipeline Pipeline;
	private GpuBuffer VertexBuffer;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
	{
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("TriangleVertexBuffer");

		// Load the shaders
		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("PositionColor.vert"),
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

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
		Pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the vertex buffer
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionColorVertex(new Vector3(-1, -1, 0), Color.Red),
				new PositionColorVertex(new Vector3( 1, -1, 0), Color.Lime),
				new PositionColorVertex(new Vector3( 0,  1, 0), Color.Blue),
			],
			BufferUsageFlags.Vertex
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta) { }

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
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.DrawPrimitives(0, 1);
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        Pipeline.Dispose();
		VertexBuffer.Dispose();
    }
}
