using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;

namespace MoonWorksGraphicsTests;

class RenderTextureMipmapsExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture Texture;

	private Sampler[] Samplers = new Sampler[5];

	private float scale = 0.5f;
	private int currentSamplerIndex = 0;
	private Color[] colors =
    [
        Color.Red,
		Color.Green,
		Color.Blue,
		Color.Yellow,
	];

	private string GetSamplerString(int index)
	{
		switch (index)
		{
			case 0:
				return "PointClamp";
			case 1:
				return "LinearClamp";
			case 2:
				return "PointClamp with Mip LOD Bias = 0.25";
			case 3:
				return "PointClamp with Min LOD = 1";
			case 4:
				return "PointClamp with Max LOD = 1";
			default:
				throw new System.Exception("Unknown sampler!");
		}
	}

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("RenderTextureMipmaps");

		Logger.LogInfo("Press Left and Right to shrink/expand the scale of the quad");
		Logger.LogInfo("Press Down to cycle through sampler states");
		Logger.LogInfo(GetSamplerString(currentSamplerIndex));

		// Load the shaders
		Shader vertShaderModule = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuadWithMatrix.vert"),
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

		// Create samplers
		SamplerCreateInfo samplerCreateInfo = SamplerCreateInfo.PointClamp;
		Samplers[0] = Sampler.Create(GraphicsDevice, samplerCreateInfo);

		samplerCreateInfo = SamplerCreateInfo.LinearClamp;
		Samplers[1] = Sampler.Create(GraphicsDevice, samplerCreateInfo);

		samplerCreateInfo = SamplerCreateInfo.PointClamp;
		samplerCreateInfo.MipLodBias = 0.25f;
		Samplers[2] = Sampler.Create(GraphicsDevice, samplerCreateInfo);

		samplerCreateInfo = SamplerCreateInfo.PointClamp;
		samplerCreateInfo.MinLod = 1;
		Samplers[3] = Sampler.Create(GraphicsDevice, samplerCreateInfo);

		samplerCreateInfo = SamplerCreateInfo.PointClamp;
		samplerCreateInfo.MaxLod = 1;
		Samplers[4] = Sampler.Create(GraphicsDevice, samplerCreateInfo);

		// Create and populate the GPU resources
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
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

		resourceUploader.Upload();
		resourceUploader.Dispose();

		Texture = Texture.Create2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler,
			4
		);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each mip level to a different color
		for (uint i = 0; i < Texture.LevelCount; i += 1)
		{
            var renderPass = cmdbuf.BeginRenderPass(new ColorTargetInfo
            {
                Texture = Texture.Handle,
                MipLevel = i,
                LoadOp = LoadOp.Clear,
                ClearColor = colors[i],
                StoreOp = StoreOp.Store
            });
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
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

		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Bottom))
		{
			currentSamplerIndex = (currentSamplerIndex + 1) % Samplers.Length;
			Logger.LogInfo(GetSamplerString(currentSamplerIndex));
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
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffers(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSamplers(new TextureSamplerBinding(Texture, Samplers[currentSamplerIndex]));
			cmdbuf.PushVertexUniformData(vertUniforms);
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

		for (var i = 0; i < 5; i += 1)
		{
			Samplers[i].Dispose();
		}
    }
}
