using System;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksGraphicsTests;

/*
 * This example builds the sprite batch using a compute shader.
 * Compare with CPUSpriteBatchExample.
 * This example should be MUCH faster.
 *
 * For speed comparisons, make sure to set the framerate to Uncapped
 * and present mode to Immediate.
*/
class ComputeSpriteBatchExample : Example
{
	ComputePipeline ComputePipeline;
	GraphicsPipeline RenderPipeline;
	Sampler Sampler;
	Texture SpriteTexture;
	TransferBuffer SpriteComputeTransferBuffer;
	Buffer SpriteComputeBuffer;
	Buffer SpriteVertexBuffer;
	Buffer SpriteIndexBuffer;

	const int MAX_SPRITE_COUNT = 8192;

	Random Random = new Random();

	[StructLayout(LayoutKind.Explicit, Size = 48)]
	struct ComputeSpriteData
	{
		[FieldOffset(0)]
		public Vector3 Position;

		[FieldOffset(12)]
		public float Rotation;

		[FieldOffset(16)]
		public Vector2 Size;

		[FieldOffset(32)]
		public Vector4 Color;
	}

	public override unsafe void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
	{
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("ComputeSpriteBatch");

		Shader vertShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadColorWithMatrix.vert"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Vertex,
				ShaderFormat = ShaderFormat.SPIRV,
				UniformBufferCount = 1
			}
		);

		Shader fragShader = new Shader(
			GraphicsDevice,
			TestUtils.GetShaderPath("TexturedQuadColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				ShaderStage = ShaderStage.Fragment,
				ShaderFormat = ShaderFormat.SPIRV,
				SamplerCount = 1
			}
		);

		GraphicsPipelineCreateInfo renderPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		renderPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>();

		RenderPipeline = new GraphicsPipeline(GraphicsDevice, renderPipelineCreateInfo);

		ComputePipeline = new ComputePipeline(
			GraphicsDevice,
			TestUtils.GetShaderPath("SpriteBatch.comp"),
			"main",
			new ComputePipelineCreateInfo
			{
				ShaderFormat = ShaderFormat.SPIRV,
				ReadOnlyStorageBufferCount = 1,
				ReadWriteStorageBufferCount = 1,
				ThreadCountX = 64,
				ThreadCountY = 1,
				ThreadCountZ = 1
			}
		);

		Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the sprite texture
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		SpriteTexture = resourceUploader.CreateTexture2DFromCompressed(TestUtils.GetTexturePath("ravioli.png"));

		resourceUploader.Upload();
		resourceUploader.Dispose();

		SpriteComputeTransferBuffer = TransferBuffer.Create<ComputeSpriteData>(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			MAX_SPRITE_COUNT
		);

		SpriteComputeBuffer = Buffer.Create<ComputeSpriteData>(
			GraphicsDevice,
			BufferUsageFlags.ComputeStorageRead,
			MAX_SPRITE_COUNT
		);

		SpriteVertexBuffer = Buffer.Create<PositionTextureColorVertex>(
			GraphicsDevice,
			BufferUsageFlags.ComputeStorageWrite | BufferUsageFlags.Vertex,
			MAX_SPRITE_COUNT * 4
		);

		SpriteIndexBuffer = Buffer.Create<uint>(
			GraphicsDevice,
			BufferUsageFlags.Index,
			MAX_SPRITE_COUNT * 6
		);

		TransferBuffer spriteIndexTransferBuffer = TransferBuffer.Create<uint>(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			MAX_SPRITE_COUNT * 6
		);

		spriteIndexTransferBuffer.Map(false, out byte* mapPointer);
		uint *indexPointer = (uint*) mapPointer;

		for (uint i = 0, j = 0; i < MAX_SPRITE_COUNT * 6; i += 6, j += 4)
		{
			indexPointer[i]     =  j;
			indexPointer[i + 1] =  j + 1;
			indexPointer[i + 2] =  j + 2;
			indexPointer[i + 3] =  j + 3;
			indexPointer[i + 4] =  j + 2;
			indexPointer[i + 5] =  j + 1;
		}
		spriteIndexTransferBuffer.Unmap();

		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer(spriteIndexTransferBuffer, SpriteIndexBuffer, false);
		cmdbuf.EndCopyPass(copyPass);
		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(TimeSpan delta)
	{

	}

	public override unsafe void Draw(double alpha)
	{
		Matrix4x4 cameraMatrix =
			Matrix4x4.CreateOrthographicOffCenter(
				0,
				640,
				480,
				0,
				0,
				-1f
			);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			// Build sprite compute transfer
			SpriteComputeTransferBuffer.Map(true, out byte* mapPointer);
			ComputeSpriteData *dataPointer = (ComputeSpriteData*) mapPointer;

			for (var i = 0; i < MAX_SPRITE_COUNT; i += 1)
			{
				dataPointer[i] = new ComputeSpriteData
				{
					Position = new Vector3(Random.Next(640), Random.Next(480), 0),
					Rotation = (float) (Random.NextDouble() * System.Math.PI * 2),
					Size = new Vector2(32, 32),
					Color = new Vector4(1f, 1f, 1f, 1f)
				};
			}
			SpriteComputeTransferBuffer.Unmap();

			// Upload compute data to buffer
			var copyPass = cmdbuf.BeginCopyPass();
			copyPass.UploadToBuffer(SpriteComputeTransferBuffer, SpriteComputeBuffer, true);
			cmdbuf.EndCopyPass(copyPass);

			// Set up compute pass to build sprite vertex buffer
			var computePass = cmdbuf.BeginComputePass(new StorageBufferReadWriteBinding
			{
				Buffer = SpriteVertexBuffer,
				Cycle = true
			});

			computePass.BindComputePipeline(ComputePipeline);
			computePass.BindStorageBuffer(SpriteComputeBuffer);
			computePass.Dispatch(MAX_SPRITE_COUNT / 64, 1, 1);

			cmdbuf.EndComputePass(computePass);

			// Render sprites using vertex buffer
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(swapchainTexture, false, Color.Black)
			);

			cmdbuf.PushVertexUniformData(cameraMatrix);

			renderPass.BindGraphicsPipeline(RenderPipeline);
			renderPass.BindVertexBuffer(SpriteVertexBuffer);
			renderPass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(SpriteTexture, Sampler));
			renderPass.DrawIndexedPrimitives(0, 0, MAX_SPRITE_COUNT * 2);

			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Destroy()
	{
		ComputePipeline.Dispose();
		RenderPipeline.Dispose();
		Sampler.Dispose();
		SpriteTexture.Dispose();
		SpriteComputeTransferBuffer.Dispose();
		SpriteComputeBuffer.Dispose();
		SpriteVertexBuffer.Dispose();
		SpriteIndexBuffer.Dispose();
	}
}
