using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;

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
		Shader vertShaderModule = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuad.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader fragShaderModule = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuad.frag"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Fragment
		);

		// Create the graphics pipeline
		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShaderModule,
			fragShaderModule
		);
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		Pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create sampler
		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);

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
				new ColorTargetInfo(swapchainTexture, LoadOp.DontCare)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffers(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSamplers(new TextureSamplerBinding(Textures[CurrentTextureIndex], Sampler));
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
		Sampler.Dispose();

		for (int i = 0; i < TextureNames.Length; i += 1)
		{
			Textures[i].Dispose();
		}
    }
}
