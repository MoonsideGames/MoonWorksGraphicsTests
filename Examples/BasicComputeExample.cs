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
	private GpuBuffer VertexBuffer;

	public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
	{
		Window = window;
		GraphicsDevice = graphicsDevice;

		// Create the compute pipeline that writes texture data
		Shader fillTextureComputeShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("FillTexture.comp"),
			"main",
			ShaderStage.Compute,
			ShaderFormat.SPIRV
		);

		ComputePipeline fillTextureComputePipeline = new ComputePipeline(
			GraphicsDevice,
			fillTextureComputeShader,
			new ComputePipelineResourceInfo { ReadWriteStorageTextureCount = 1 }
		);

		fillTextureComputeShader.Dispose();

		// Create the compute pipeline that calculates squares of numbers
		Shader calculateSquaresComputeShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("CalculateSquares.comp"),
			"main",
			ShaderStage.Compute,
			ShaderFormat.SPIRV
		);

		ComputePipeline calculateSquaresComputePipeline = new ComputePipeline(
			GraphicsDevice,
			calculateSquaresComputeShader,
			new ComputePipelineResourceInfo { ReadWriteStorageBufferCount = 1 }
		);

		calculateSquaresComputeShader.Dispose();

		// Create the graphics pipeline
		Shader vertShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			ShaderStage.Vertex,
			ShaderFormat.SPIRV
		);

		Shader fragShaderModule = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.frag"),
			"main",
			ShaderStage.Fragment,
			ShaderFormat.SPIRV
		);

		GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShaderModule,
			fragShaderModule
		);
		drawPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
		drawPipelineCreateInfo.FragmentShaderResourceInfo = new GraphicsPipelineResourceInfo{
			SamplerCount = 1
		};

		DrawPipeline = new GraphicsPipeline(
			GraphicsDevice,
			drawPipelineCreateInfo
		);

		// Create buffers and textures
		uint[] squares = new uint[64];
		GpuBuffer squaresBuffer = GpuBuffer.Create<uint>(
			GraphicsDevice,
			BufferUsageFlags.ComputeStorageWrite,
			(uint) squares.Length
		);

		TransferBuffer transferBuffer = new TransferBuffer(
			GraphicsDevice,
			TransferUsage.Buffer,
			TransferBufferMapFlags.Read,
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

		copyPass.DownloadFromBuffer(squaresBuffer, transferBuffer, new BufferCopy(0, 0, squaresBuffer.Size));

		cmdbuf.EndCopyPass(copyPass);

		var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFences(fence);
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
