using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests;

class BasicComputeExample : Example
{
	private GraphicsPipeline DrawPipeline;
	private Texture Texture;
	private Sampler Sampler;
	private Buffer VertexBuffer;

	public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
	{
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("BasicCompute");

        // Create the compute pipeline that writes texture data
        ComputePipeline fillTextureComputePipeline = new ComputePipeline(
			GraphicsDevice,
			TestUtils.GetShaderPath("FillTexture.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				ShaderFormat = ShaderFormat.SPIRV,
				ReadWriteStorageTextureCount = 1,
				ThreadCountX = 8,
				ThreadCountY = 8,
				ThreadCountZ = 1
			}
		);

        // Create the compute pipeline that calculates squares of numbers
        ComputePipeline calculateSquaresComputePipeline = new ComputePipeline(
			GraphicsDevice,
			TestUtils.GetShaderPath("CalculateSquares.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				ShaderFormat = ShaderFormat.SPIRV,
				ReadWriteStorageBufferCount = 1,
				ThreadCountX = 8,
				ThreadCountY = 1,
				ThreadCountZ = 1
			}
		);

		// Create the graphics pipeline
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
			TestUtils.GetShaderPath("TexturedQuad.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1
			}
		);

		GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		drawPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

		DrawPipeline = new GraphicsPipeline(
			GraphicsDevice,
			drawPipelineCreateInfo
		);

		// Create buffers and textures
		uint[] squares = new uint[64];
		Buffer squaresBuffer = Buffer.Create<uint>(
			GraphicsDevice,
			BufferUsageFlags.ComputeStorageWrite,
			(uint) squares.Length
		);

		TransferBuffer transferBuffer = new TransferBuffer(
			GraphicsDevice,
			TransferBufferUsage.Download,
			squaresBuffer.Size
		);

		Texture = Texture.CreateTexture2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.ComputeStorageWrite | TextureUsageFlags.Sampler
		);

		Sampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

		// Upload GPU resources and dispatch compute work
		var resourceUploader = new ResourceUploader(GraphicsDevice);
		VertexBuffer = resourceUploader.CreateBuffer(
			[
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
			],
			BufferUsageFlags.Vertex
		);

		resourceUploader.Upload();
		resourceUploader.Dispose();

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// This should result in a bright yellow texture!
		var computePass = cmdbuf.BeginComputePass(new StorageTextureReadWriteBinding
		{
			TextureSlice = Texture,
			Cycle = false
		});

		computePass.BindComputePipeline(fillTextureComputePipeline);
		computePass.Dispatch(Texture.Width / 8, Texture.Height / 8, 1);

		cmdbuf.EndComputePass(computePass);

		// This calculates the squares of the first N integers!
		computePass = cmdbuf.BeginComputePass(new StorageBufferReadWriteBinding
		{
			Buffer = squaresBuffer,
			Cycle = false
		});

		computePass.BindComputePipeline(calculateSquaresComputePipeline);
		computePass.Dispatch((uint) squares.Length / 8, 1, 1);

		cmdbuf.EndComputePass(computePass);

		var copyPass = cmdbuf.BeginCopyPass();

		copyPass.DownloadFromBuffer(
			new BufferRegion(squaresBuffer, 0, squaresBuffer.Size),
			new TransferBufferLocation(transferBuffer)
		);

		cmdbuf.EndCopyPass(copyPass);

		var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Print the squares!
		transferBuffer.GetData<uint>(squares, 0);
		Logger.LogInfo("Squares of the first " + squares.Length + " integers: " + string.Join(", ", squares));
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchainTexture, false, Color.CornflowerBlue));
			renderPass.BindGraphicsPipeline(DrawPipeline);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture, Sampler));
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.DrawPrimitives(0, 2);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Destroy()
	{
		DrawPipeline.Dispose();
		Texture.Dispose();
		Sampler.Dispose();
		VertexBuffer.Dispose();
	}
}
