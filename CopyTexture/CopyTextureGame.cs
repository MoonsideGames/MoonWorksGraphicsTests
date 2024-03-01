using System.Runtime.InteropServices;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class CopyTextureGame : Game
	{
		private GraphicsPipeline pipeline;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;
		private Texture originalTexture;
		private Texture textureCopy;
		private Texture textureSmallCopy;
		private Sampler sampler;

		public unsafe CopyTextureGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.AttachmentInfo.ColorAttachmentDescriptions[0].BlendState = ColorAttachmentBlendState.AlphaBlend;
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create sampler
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1f, 0f, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3( 0f, 0f, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3( 0f, 1f, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1f, 1f, 0), new Vector2(0, 1)),

					new PositionTextureVertex(new Vector3(0f, 0f, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1f, 0f, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1f, 1f, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(0f, 1f, 0), new Vector2(0, 1)),

					new PositionTextureVertex(new Vector3(-0.5f, -1f, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3( 0.5f, -1f, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3( 0.5f,  0f, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-0.5f,  0f, 0), new Vector2(0, 1))
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0, 1, 2,
					0, 2, 3,
				],
				BufferUsageFlags.Index
			);

			originalTexture = resourceUploader.CreateTexture2DFromCompressed(
				TestUtils.GetTexturePath("ravioli.png")
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();

			// Load the texture bytes so we can compare them.
			var pixels = ImageUtils.GetPixelDataFromFile(
				TestUtils.GetTexturePath("ravioli.png"),
				out var width,
				out var height,
				out var byteCount
			);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			var textureCreateInfo = new TextureCreateInfo
			{
				Width = originalTexture.Width,
				Height = originalTexture.Height,
				Depth = originalTexture.Depth,
				IsCube = originalTexture.IsCube,
				LayerCount = originalTexture.LayerCount,
				LevelCount = originalTexture.LevelCount,
				SampleCount = originalTexture.SampleCount,
				Format = originalTexture.Format,
				UsageFlags = originalTexture.UsageFlags
			};

			// Create a 1:1 copy of the texture
			textureCopy = new Texture(GraphicsDevice, textureCreateInfo);

			cmdbuf.BeginCopyPass();
			cmdbuf.CopyTextureToTexture(
				originalTexture,
				textureCopy,
				WriteOptions.SafeOverwrite
			);
			cmdbuf.EndCopyPass();

			// Create a half-sized copy of this texture
			textureCreateInfo.Width /= 2;
			textureCreateInfo.Height /= 2;
			textureCreateInfo.UsageFlags |= TextureUsageFlags.ColorTarget;
			textureSmallCopy = new Texture(GraphicsDevice, textureCreateInfo);

			// Render the half-size copy
			cmdbuf.Blit(originalTexture, textureSmallCopy, Filter.Linear, WriteOptions.SafeOverwrite);

			// Copy the texture to a transfer buffer
			TransferBuffer compareBuffer = new TransferBuffer(GraphicsDevice, byteCount);

			cmdbuf.BeginCopyPass();
			cmdbuf.DownloadFromTexture(
				originalTexture,
				compareBuffer,
				new BufferImageCopy(0, 0, 0),
				TransferOptions.Overwrite
			);
			cmdbuf.EndCopyPass();

			var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Compare the original bytes to the copied bytes.
			var copiedBytes = NativeMemory.Alloc(byteCount);
			var copiedSpan = new System.Span<byte>(copiedBytes, (int) byteCount);
			compareBuffer.GetData(copiedSpan);

			var originalSpan = new System.Span<byte>((void*) pixels, (int)byteCount);

			if (System.MemoryExtensions.SequenceEqual(originalSpan, copiedSpan))
			{
				Logger.LogError("SUCCESS! Original texture bytes and the bytes from CopyTextureToBuffer match!");

			}
			else
			{
				Logger.LogError("FAIL! Original texture bytes do not match bytes from CopyTextureToBuffer!");
			}

			RefreshCS.Refresh.Refresh_Image_Free(pixels);
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(originalTexture, sampler));
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(textureCopy, sampler));
				cmdbuf.DrawIndexedPrimitives(4, 0, 2);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(textureSmallCopy, sampler));
				cmdbuf.DrawIndexedPrimitives(8, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			CopyTextureGame game = new CopyTextureGame();
			game.Run();
		}
	}
}
