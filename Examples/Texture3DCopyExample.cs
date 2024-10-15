using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class Texture3DCopyExample : Example
{
	private GraphicsPipeline Pipeline;
	private Buffer VertexBuffer;
	private Buffer IndexBuffer;
	private Texture RenderTexture;
	private Texture Texture3D;
	private Sampler Sampler;

	private float t;
	private Color[] colors =
    [
        Color.Red,
		Color.Green,
		Color.Blue,
	];

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

		Window.SetTitle("Texture3DCopy");

		// Load the shaders
		Shader vertShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV
			}
		);
		Shader fragShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad3D.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV,
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
		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);

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

		RenderTexture = Texture.Create2DArray(
			GraphicsDevice,
			16,
			16,
			(uint) colors.Length,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		Texture3D = Texture.Create3D(
			GraphicsDevice,
			16,
			16,
			3,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.Sampler
		);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each layer slice of the RT to a different color
		for (uint i = 0; i < colors.Length; i += 1)
		{
            var renderPass = cmdbuf.BeginRenderPass(new ColorTargetInfo
            {
                Texture = RenderTexture.Handle,
                LayerOrDepthPlane = i,
                LoadOp = LoadOp.Clear,
                ClearColor = colors[i],
                StoreOp = StoreOp.Store
            });
			cmdbuf.EndRenderPass(renderPass);
		}

		// Copy each layer slice to a different 3D depth
		var copyPass = cmdbuf.BeginCopyPass();
		for (var i = 0; i < 3; i += 1)
		{
			copyPass.CopyTextureToTexture(
				new TextureLocation
				{
					Texture = RenderTexture.Handle,
					Layer = (uint) i
				},
				new TextureLocation
				{
					Texture = Texture3D.Handle,
					Z = (uint) i
				},
				16,
				16,
				1,
				false
			);
		}
		cmdbuf.EndCopyPass(copyPass);

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(System.TimeSpan delta)
	{
		t += (float) delta.TotalSeconds;
	}

	public override void Draw(double alpha)
	{
		FragUniform fragUniform = new FragUniform(t);

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
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture3D, Sampler));
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
		RenderTexture.Dispose();
		Texture3D.Dispose();
		Sampler.Dispose();
    }
}
