using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class Texture3DExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture Texture;
	private Sampler Sampler;

	private int currentDepth = 0;

	struct FragUniform
	{
		public float Depth;

		public FragUniform(float depth)
		{
			Depth = depth;
		}
	}

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("Texture3D");

		Logger.LogInfo("Press Left and Right to cycle between depth slices");

		// Load the shaders
		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV
			}
		);

		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad3D.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1,
				UniformBufferCount = 1
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

		// Create samplers
		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

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

		Texture = Texture.CreateTexture3D(
			GraphicsDevice,
			16,
			16,
			7,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.Sampler
		);

		// Load each depth subimage of the 3D texture
		for (uint i = 0; i < Texture.Depth; i += 1)
		{
			var region = new TextureRegion
			{
				TextureSlice = new TextureSlice
				{
					Texture = Texture,
					MipLevel = 0,
					Layer = 0
				},
				X = 0,
				Y = 0,
				Z = i,
				Width = Texture.Width,
				Height = Texture.Height,
				Depth = 1
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
				currentDepth = (int) Texture.Depth - 1;
			}
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			currentDepth += 1;
			if (currentDepth >= Texture.Depth)
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
		FragUniform fragUniform = new FragUniform((float)currentDepth / Texture.Depth + 0.01f);

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
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture, Sampler));
			renderPass.PushFragmentUniformData(fragUniform);
			renderPass.DrawIndexedPrimitives(0, 0, 2);
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
