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
        ComputePipeline fillTextureComputePipeline = ComputePipeline.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("FillTexture.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				Format = ShaderFormat.SPIRV,
				NumReadWriteStorageTextures = 1,
				ThreadCountX = 8,
				ThreadCountY = 8,
				ThreadCountZ = 1
			}
		);

        // Create the compute pipeline that calculates squares of numbers
        ComputePipeline calculateSquaresComputePipeline = ComputePipeline.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("CalculateSquares.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				Format = ShaderFormat.SPIRV,
				NumReadWriteStorageBuffers = 1,
				ThreadCountX = 8,
				ThreadCountY = 1,
				ThreadCountZ = 1
			}
		);

		// Create the graphics pipeline
		Shader vertShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV
			}
		);

		Shader fragShader = Shader.CreateFromFile(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuad.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV,
				NumSamplers = 1
			}
		);

		GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		drawPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>(0);

		DrawPipeline = GraphicsPipeline.Create(
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

		TransferBuffer transferBuffer = TransferBuffer.Create<uint>(
			GraphicsDevice,
			TransferBufferUsage.Download,
			(uint) squares.Length
		);

		Texture = Texture.Create2D(
			GraphicsDevice,
			Window.Width,
			Window.Height,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.ComputeStorageWrite | TextureUsageFlags.Sampler
		);

		Sampler = Sampler.Create(GraphicsDevice, new SamplerCreateInfo());

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
			Texture = Texture.Handle,
			Cycle = false
		});

		computePass.BindComputePipeline(fillTextureComputePipeline);
		computePass.Dispatch(Texture.Width / 8, Texture.Height / 8, 1);

		cmdbuf.EndComputePass(computePass);

		// This calculates the squares of the first N integers!
		computePass = cmdbuf.BeginComputePass(new StorageBufferReadWriteBinding
		{
			Buffer = squaresBuffer.Handle,
			Cycle = false
		});

		computePass.BindComputePipeline(calculateSquaresComputePipeline);
		computePass.Dispatch((uint) squares.Length / 8, 1, 1);

		cmdbuf.EndComputePass(computePass);

		var copyPass = cmdbuf.BeginCopyPass();

		copyPass.DownloadFromBuffer(
			new BufferRegion
			{
				Buffer = squaresBuffer.Handle,
				Size = squaresBuffer.Size
			},
			new TransferBufferLocation
			{
				TransferBuffer = transferBuffer.Handle
			}
		);

		cmdbuf.EndCopyPass(copyPass);

		var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Print the squares!
		var transferSpan = transferBuffer.Map<uint>(false);
		transferSpan.CopyTo(squares);
		Logger.LogInfo("Squares of the first " + squares.Length + " integers: " + string.Join(", ", squares));
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(new ColorTargetInfo
			{
				Texture = swapchainTexture.Handle,
				LoadOp = LoadOp.Clear,
				ClearColor = Color.CornflowerBlue
			});
			renderPass.BindGraphicsPipeline(DrawPipeline);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(Texture, Sampler));
			renderPass.BindVertexBuffer(VertexBuffer);
			renderPass.DrawPrimitives(6, 1, 0, 0);
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
