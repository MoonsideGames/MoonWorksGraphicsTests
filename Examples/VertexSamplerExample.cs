using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksGraphicsTests;

class VertexSamplerExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Texture Texture;
	private Sampler Sampler;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("VertexSampler");

		// Load the shaders
		Shader vertShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("PositionSampler.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV,
				NumSamplers = 1
			}
		);

		Shader fragShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create and populate the GPU resources
		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

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
			[Color.Yellow, Color.Indigo, Color.HotPink],
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.Sampler,
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
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindVertexSampler(new TextureSamplerBinding(Texture, Sampler));
			renderPass.DrawPrimitives(3, 1, 0, 0);
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
