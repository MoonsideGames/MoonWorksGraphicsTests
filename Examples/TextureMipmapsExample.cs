using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class TextureMipmapsExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture Texture;
	private Sampler Sampler;

	private float scale = 0.5f;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("TextureMipmaps");

		Logger.LogInfo("Press Left and Right to shrink/expand the scale of the quad");

		// Load the shaders
		Shader vertShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadWithMatrix.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV,
				UniformBufferCount = 1
			}
		);

		Shader fragShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1
			}
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShaderModule,
			fragShaderModule
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		Texture = Texture.CreateTexture2D(
			GraphicsDevice,
			256,
			256,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.Sampler,
			4
		);

		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1,  1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3( 1,  1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3( 1, -1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)),
			],
			BufferUsageFlags.Vertex
		);

		IndexBuffer = resourceUploader.CreateBuffer<ushort>(
			[
				0, 1, 2,
				0, 2, 3
			],
			BufferUsageFlags.Index
		);

		// Set the various mip levels
		for (uint i = 0; i < Texture.LevelCount; i += 1)
		{
			var w = Texture.Width >> (int) i;
			var h = Texture.Height >> (int) i;
			var region = new TextureRegion
			{
				TextureSlice = new TextureSlice
				{
					Texture = Texture,
					Layer = 0,
					MipLevel = i
				},
				X = 0,
				Y = 0,
				Z = 0,
				Width = w,
				Height = h,
				Depth = 1
			};

			resourceUploader.SetTextureDataFromCompressed(
				region,
				TestUtils.GetTexturePath($"mip{i}.png")
			);
		}

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Left))
		{
			scale = System.MathF.Max(0.01f, scale - 0.01f);
		}

		if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Right))
		{
			scale = System.MathF.Min(1f, scale + 0.01f);
		}
	}

	public override void Draw(double alpha)
	{
		TransformVertexUniform vertUniforms = new TransformVertexUniform(Matrix4x4.CreateScale(scale, scale, 1));

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
			renderPass.PushVertexUniformData(vertUniforms);
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
