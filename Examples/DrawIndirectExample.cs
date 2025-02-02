using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;

namespace MoonWorksGraphicsTests;

class DrawIndirectExample : Example
{
	private GraphicsPipeline GraphicsPipeline;
	private Buffer VertexBuffer;
	private Buffer DrawBuffer;

    public override void Init()
    {
		Window.SetTitle("DrawIndirect");

		// Load the shaders
		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			RootTitleStorage,
			TestUtils.GetHLSLPath("PositionColor.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			RootTitleStorage,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Fragment
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>();
		GraphicsPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the vertex buffer
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			"Vertex Buffer",
			[
				new PositionColorVertex(new Vector3(-0.5f,  1, 0), Color.Blue),
				new PositionColorVertex(new Vector3(  -1f, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3(   0f, -1, 0), Color.Red),

				new PositionColorVertex(new Vector3(0.5f,  1, 0), Color.Blue),
				new PositionColorVertex(new Vector3(  1f, -1, 0), Color.Green),
				new PositionColorVertex(new Vector3(  0f, -1, 0), Color.Red),
			],
			BufferUsageFlags.Vertex
		);

		DrawBuffer = resourceUploader.CreateBuffer(
			"Draw Buffer",
			[
				new IndirectDrawCommand
				{
					NumVertices = 3,
					NumInstances = 1,
					FirstVertex = 3
				},
				new IndirectDrawCommand
				{
					NumVertices = 3,
					NumInstances = 1
				}
			],
			BufferUsageFlags.Indirect
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
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(GraphicsPipeline);
			renderPass.BindVertexBuffers(VertexBuffer);
			renderPass.DrawPrimitivesIndirect(DrawBuffer, 0, 2);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        GraphicsPipeline.Dispose();
		VertexBuffer.Dispose();
		DrawBuffer.Dispose();
    }
}
