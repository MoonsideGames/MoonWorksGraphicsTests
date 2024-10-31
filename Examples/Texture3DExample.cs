using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;

namespace MoonWorksGraphicsTests;

class Texture3DExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture Texture;
	private Sampler Sampler;

	private int currentDepth = 0;

	readonly record struct FragUniform(float Depth);

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("Texture3D");

		Logger.LogInfo("Press Left and Right to cycle between depth slices");

		// Load the shaders
		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuad.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Vertex
			}
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuad3D.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Fragment,
				NumSamplers = 1,
				NumUniformBuffers = 1
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

		// Create samplers
		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1))
			],
			BufferUsageFlags.Vertex
		);

		IndexBuffer = resourceUploader.CreateBuffer<ushort>(
			[
				0, 1, 2,
				0, 2, 3,
			],
			BufferUsageFlags.Index
		);

		Texture = Texture.Create3D(
			GraphicsDevice,
			16,
			16,
			7,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.Sampler
		);

		// Load each depth subimage of the 3D texture
		for (uint i = 0; i < Texture.LayerCountOrDepth; i += 1)
		{
			var region = new TextureRegion
			{
				Texture = Texture.Handle,
				Z = i,
				W = Texture.Width,
				H = Texture.Height,
				D = 1
			};

			resourceUploader.SetTextureDataFromCompressed(
				region,
				TestUtils.GetTexturePath($"tex3d_{i}.png")
			);
		}

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		int prevDepth = currentDepth;

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			currentDepth -= 1;
			if (currentDepth < 0)
			{
				currentDepth = (int) Texture.LayerCountOrDepth - 1;
			}
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			currentDepth += 1;
			if (currentDepth >= Texture.LayerCountOrDepth)
			{
				currentDepth = 0;
			}
		}

		if (prevDepth != currentDepth)
		{
			Logger.LogInfo("Setting depth to: " + currentDepth);
		}
	}

	public override void Draw(double alpha)
	{
		FragUniform fragUniform = new FragUniform((float)currentDepth / Texture.LayerCountOrDepth + 0.01f);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture, Sampler));
			cmdbuf.PushFragmentUniformData(fragUniform);
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        Pipeline.Dispose();
		VertexBuffer.Dispose();
		IndexBuffer.Dispose();
		Texture.Dispose();
		Sampler.Dispose();
    }
}
