using System;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using System.Numerics;
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

		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuadColorWithMatrix.vert"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Vertex,
				NumUniformBuffers = 1
			}
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("TexturedQuadColor.frag"),
			"main",
			new ShaderCross.ShaderCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				Stage = ShaderStage.Fragment,
				NumSamplers = 1
			}
		);

		GraphicsPipelineCreateInfo renderPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		renderPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureColorVertex>();

		RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

		ComputePipeline = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SpriteBatch.comp"),
			"main",
			new ShaderCross.ComputePipelineCreateInfo
			{
				Format = ShaderCross.ShaderFormat.HLSL,
				NumReadonlyStorageBuffers = 1,
				NumReadWriteStorageBuffers = 1,
				ThreadCountX = 64,
				ThreadCountY = 1,
				ThreadCountZ = 1
			}
		);

		Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.PointClamp);

		// Create and populate the sprite texture
		var resourceUploader = new ResourceUploader(GraphicsDevice);

		SpriteTexture = resourceUploader.CreateTexture2DFromCompressed(
			TestUtils.GetTexturePath("ravioli.png"),
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.Sampler
		);

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

		var indexSpan = spriteIndexTransferBuffer.Map<uint>(false);

		for (int i = 0, j = 0; i < MAX_SPRITE_COUNT * 6; i += 6, j += 4)
		{
			indexSpan[i]     =  (uint) j;
			indexSpan[i + 1] =  (uint) j + 1;
			indexSpan[i + 2] =  (uint) j + 2;
			indexSpan[i + 3] =  (uint) j + 3;
			indexSpan[i + 4] =  (uint) j + 2;
			indexSpan[i + 5] =  (uint) j + 1;
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
			var data = SpriteComputeTransferBuffer.Map<ComputeSpriteData>(true);
			for (var i = 0; i < MAX_SPRITE_COUNT; i += 1)
			{
				data[i].Position = new Vector3(Random.Next(640), Random.Next(480), 0);
				data[i].Rotation = (float) (Random.NextDouble() * System.Math.PI * 2);
				data[i].Size = new Vector2(32, 32);
				data[i].Color = new Vector4(1f, 1f, 1f, 1f);
			}
			SpriteComputeTransferBuffer.Unmap();

			// Upload compute data to buffer
			var copyPass = cmdbuf.BeginCopyPass();
			copyPass.UploadToBuffer(SpriteComputeTransferBuffer, SpriteComputeBuffer, true);
			cmdbuf.EndCopyPass(copyPass);

			// Set up compute pass to build sprite vertex buffer
			var computePass = cmdbuf.BeginComputePass(
				new StorageBufferReadWriteBinding(SpriteVertexBuffer, true)
			);

			computePass.BindComputePipeline(ComputePipeline);
			computePass.BindStorageBuffer(SpriteComputeBuffer);
			computePass.Dispatch(MAX_SPRITE_COUNT / 64, 1, 1);

			cmdbuf.EndComputePass(computePass);

			// Render sprites using vertex buffer
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);

			cmdbuf.PushVertexUniformData(cameraMatrix);

			renderPass.BindGraphicsPipeline(RenderPipeline);
			renderPass.BindVertexBuffer(SpriteVertexBuffer);
			renderPass.BindIndexBuffer(SpriteIndexBuffer, IndexElementSize.ThirtyTwo);
			renderPass.BindFragmentSampler(new TextureSamplerBinding(SpriteTexture, Sampler));
			renderPass.DrawIndexedPrimitives(MAX_SPRITE_COUNT * 6, 1, 0, 0, 0);

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
