using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;

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

		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the GPU resources
		Texture = Texture.Create2D(
			GraphicsDevice,
			256,
			256,
			TextureFormat.R8G8B8A8Unorm,
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
				Texture = Texture.Handle,
				MipLevel = i,
				W = w,
				H = h,
				D = 1
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
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);
			renderPass.BindGraphicsPipeline(Pipeline);
			renderPass.BindVertexBuffers(VertexBuffer);
			renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.Sixteen);
			renderPass.BindFragmentSamplers(new TextureSamplerBinding(Texture, Sampler));
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
		Sampler.Dispose();
    }
}
