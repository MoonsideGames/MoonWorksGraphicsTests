using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class VertexSamplerExample : Example
{
	private GraphicsPipeline Pipeline;
	private GpuBuffer VertexBuffer;
	private Texture Texture;
	private Sampler Sampler;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("VertexSampler");

		// Load the shaders
		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("PositionSampler.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1
			}
		);

		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the GPU resources
		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3( 1, -1, 0), new Vector2(0.334f, 0)),
				new PositionTextureVertex(new Vector3( 0,  1, 0), new Vector2(0.667f, 0)),
			],
			BufferUsageFlags.Vertex
		);

		Texture = resourceUploader.CreateTexture2D(
			new Span<Color>([Color.Yellow, Color.Indigo, Color.HotPink]),
			3,
			1
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
			renderPass.BindVertexSampler(new TextureSamplerBinding(Texture, Sampler));
			renderPass.DrawPrimitives(0, 1);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		Pipeline.Dispose();
		VertexBuffer.Dispose();
		Texture.Dispose();
		Sampler.Dispose();
    }
}
