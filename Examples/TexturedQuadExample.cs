using System;
using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;
using System.Runtime.InteropServices;

namespace MoonWorksGraphicsTests;

class TexturedQuadExample : Example
{
	private GraphicsPipeline pipeline;
	private Buffer vertexBuffer;
	private Buffer indexBuffer;
	private Sampler[] samplers = new Sampler[6];
	private string[] samplerNames =
    [
        "PointClamp",
		"PointWrap",
		"LinearClamp",
		"LinearWrap",
		"AnisotropicClamp",
		"AnisotropicWrap"
	];

	private int currentSamplerIndex;

	private Texture[] textures = new Texture[4];
	private string[] imageLoadFormatNames =
    [
        "PNG from file",
		"PNG from memory",
		"QOI from file",
		"QOI from memory"
	];

	private int currentTextureIndex;

	private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    public override unsafe void Init()
    {
		Window.SetTitle("TexturedQuad");

		Logger.LogInfo("Press Left and Right to cycle between sampler states");
		Logger.LogInfo("Setting sampler state to: " + samplerNames[0]);

		Logger.LogInfo("Press Down to cycle between image load formats");
		Logger.LogInfo("Setting image format to: " + imageLoadFormatNames[0]);

		var pngPath = TestUtils.GetTexturePath("ravioli.png");
		var qoiPath = TestUtils.GetTexturePath("ravioli.qoi");

		RootTitleStorage.GetFileSize(pngPath, out var pngSize);
		RootTitleStorage.GetFileSize(qoiPath, out var qoiSize);

		var pngBytes = NativeMemory.Alloc((nuint) pngSize);
		var pngSpan = new Span<byte>(pngBytes, (int) pngSize);

		var qoiBytes = NativeMemory.Alloc((nuint) qoiSize);
		var qoiSpan = new Span<byte>(qoiBytes, (int) qoiSize);

		RootTitleStorage.ReadFile(pngPath, pngSpan);
		RootTitleStorage.ReadFile(qoiPath, qoiSpan);

		Logger.LogInfo(pngSpan.Length.ToString());
		Logger.LogInfo(qoiSpan.Length.ToString());

		// Load the shaders
		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			RootTitleStorage,
			TestUtils.GetHLSLPath("TexturedQuad.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			RootTitleStorage,
			TestUtils.GetHLSLPath("TexturedQuad.frag"),
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
		pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		pipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

		// Create samplers
		samplers[0] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);
		samplers[1] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointWrap);
		samplers[2] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);
		samplers[3] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);
		samplers[4] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.AnisotropicClamp);
		samplers[5] = Sampler.Create(GraphicsDevice, SamplerCreateInfo.AnisotropicWrap);

        ReadOnlySpan<PositionTextureVertex> vertexData = [
			new PositionTextureVertex(new Vector3(-1,  1, 0), new Vector2(0, 0)),
			new PositionTextureVertex(new Vector3( 1,  1, 0), new Vector2(4, 0)),
			new PositionTextureVertex(new Vector3( 1, -1, 0), new Vector2(4, 4)),
			new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 4)),
		];

        ReadOnlySpan<ushort> indexData = [
			0, 1, 2,
			0, 2, 3,
		];

		// Create and populate the GPU resources

		var resourceUploader = new ResourceUploader(GraphicsDevice);

		vertexBuffer = resourceUploader.CreateBuffer(vertexData, BufferUsageFlags.Vertex);
		indexBuffer = resourceUploader.CreateBuffer(indexData, BufferUsageFlags.Index);

		textures[0] = resourceUploader.CreateTexture2DFromCompressed(RootTitleStorage, TestUtils.GetTexturePath("ravioli.png"), TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
		textures[1] = resourceUploader.CreateTexture2DFromCompressed(pngSpan, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
		textures[2] = resourceUploader.CreateTexture2DFromCompressed(RootTitleStorage, TestUtils.GetTexturePath("ravioli.qoi"), TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
		textures[3] = resourceUploader.CreateTexture2DFromCompressed(qoiSpan, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);

		resourceUploader.Upload();
		resourceUploader.Dispose();

		NativeMemory.Free(pngBytes);
		NativeMemory.Free(qoiBytes);
	}

	public override void Update(System.TimeSpan delta)
	{
		int prevSamplerIndex = currentSamplerIndex;

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
		{
			currentSamplerIndex -= 1;
			if (currentSamplerIndex < 0)
			{
				currentSamplerIndex = samplers.Length - 1;
			}
		}

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
		{
			currentSamplerIndex += 1;
			if (currentSamplerIndex >= samplers.Length)
			{
				currentSamplerIndex = 0;
			}
		}

		if (prevSamplerIndex != currentSamplerIndex)
		{
			Logger.LogInfo("Setting sampler state to: " + samplerNames[currentSamplerIndex]);
		}

		int prevTextureIndex = currentTextureIndex;

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
		{
			currentTextureIndex = (currentTextureIndex + 1) % imageLoadFormatNames.Length;
		}

		if (prevTextureIndex != currentTextureIndex)
		{
			Logger.LogInfo("Setting texture format to: " + imageLoadFormatNames[currentTextureIndex]);
		}
	}

	public override void Draw(double alpha)
	{
		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(pipeline);
			renderPass.BindVertexBuffers(vertexBuffer);
			renderPass.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSamplers(new TextureSamplerBinding(textures[currentTextureIndex], samplers[currentSamplerIndex]));
			renderPass.DrawIndexedPrimitives(6, 1, 0, 0, 0);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        pipeline.Dispose();
		vertexBuffer.Dispose();
		indexBuffer.Dispose();

		for (var i = 0; i < samplers.Length; i += 1)
		{
			samplers[i].Dispose();
		}

		for (var i = 0; i < textures.Length; i += 1)
		{
			textures[i].Dispose();
		}
    }
}
