using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class CompressedTexturesExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Sampler Sampler;
	private Texture[] Textures;
	private string[] TextureNames =
	[
		"BC1",
		"BC2",
		"BC3",
		"BC7"
	];

	private int CurrentTextureIndex;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("CompressedTextures");

		Logger.LogInfo("Press Left and Right to cycle between textures");
		Logger.LogInfo("Setting texture to: " + TextureNames[0]);

		// Load the shaders
		Shader vertShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV
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

		// Create sampler
		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);

		// Create texture array
		Textures = new Texture[TextureNames.Length];

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1,  1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3( 1,  1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3( 1, -1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1))
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

		for (int i = 0; i < TextureNames.Length; i += 1)
		{
			Logger.LogInfo(TextureNames[i]);
			Textures[i] = resourceUploader.CreateTextureFromDDS(TestUtils.GetTexturePath(TextureNames[i] + ".dds"));
		}

		resourceUploader.Upload();
		resourceUploader.Dispose();
	}

	public override void Update(System.TimeSpan delta)
	{
		int prevSamplerIndex = CurrentTextureIndex;

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			CurrentTextureIndex -= 1;
			if (CurrentTextureIndex < 0)
			{
				CurrentTextureIndex = TextureNames.Length - 1;
			}
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			CurrentTextureIndex += 1;
			if (CurrentTextureIndex >= TextureNames.Length)
			{
				CurrentTextureIndex = 0;
			}
		}

		if (prevSamplerIndex != CurrentTextureIndex)
		{
			Logger.LogInfo("Setting texture to: " + TextureNames[CurrentTextureIndex]);
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
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Textures[CurrentTextureIndex], Sampler));
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
		Sampler.Dispose();

		for (int i = 0; i < TextureNames.Length; i += 1)
		{
			Textures[i].Dispose();
		}
    }
}
