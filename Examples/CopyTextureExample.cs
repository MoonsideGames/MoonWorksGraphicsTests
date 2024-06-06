using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;

namespace MoonWorksGraphicsTests
{
	class CopyTextureExample : Example
	{
		private Texture OriginalTexture;
		private Texture TextureCopy;
		private Texture TextureSmall;

        public override unsafe void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
        {
			Window = window;
			GraphicsDevice = graphicsDevice;

			Window.SetTitle("CopyTexture");

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			OriginalTexture = resourceUploader.CreateTexture2DFromCompressed(
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
				Width = OriginalTexture.Width,
				Height = OriginalTexture.Height,
				Depth = OriginalTexture.Depth,
				IsCube = OriginalTexture.IsCube,
				LayerCount = OriginalTexture.LayerCount,
				LevelCount = OriginalTexture.LevelCount,
				SampleCount = OriginalTexture.SampleCount,
				Format = OriginalTexture.Format,
				UsageFlags = OriginalTexture.UsageFlags
			};

			// Create a 1:1 copy of the texture
			TextureCopy = new Texture(GraphicsDevice, textureCreateInfo);

			// Create a download transfer buffer
			TransferBuffer compareBuffer = new TransferBuffer(
				GraphicsDevice,
				TransferUsage.Texture,
				TransferBufferMapFlags.Read,
				byteCount
			);

			var copyPass = cmdbuf.BeginCopyPass();
			copyPass.CopyTextureToTexture(
				OriginalTexture,
				TextureCopy,
				false
			);
			cmdbuf.EndCopyPass(copyPass);

			// Create a half-sized copy of this texture
			textureCreateInfo.Width /= 2;
			textureCreateInfo.Height /= 2;
			textureCreateInfo.UsageFlags |= TextureUsageFlags.ColorTarget;
			TextureSmall = new Texture(GraphicsDevice, textureCreateInfo);

			// Render the half-size copy
			cmdbuf.Blit(OriginalTexture, TextureSmall, Filter.Linear, false);

			// Copy the texture to a transfer buffer
			copyPass = cmdbuf.BeginCopyPass();
			copyPass.DownloadFromTexture(
				TextureCopy,
				compareBuffer,
				new BufferImageCopy(0, 0, 0)
			);
			cmdbuf.EndCopyPass(copyPass);

			var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFence(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Compare the original bytes to the copied bytes.
			var copiedBytes = NativeMemory.Alloc(byteCount);
			var copiedSpan = new System.Span<byte>(copiedBytes, (int) byteCount);
			compareBuffer.GetData(copiedSpan);

			var originalSpan = new System.Span<byte>(pixels, (int)byteCount);

			if (System.MemoryExtensions.SequenceEqual(originalSpan, copiedSpan))
			{
				Logger.LogInfo("SUCCESS! Original texture bytes and the downloaded bytes match!");

			}
			else
			{
				Logger.LogError("FAIL! Original texture bytes do not match downloaded bytes!");
			}

			RefreshCS.Refresh.Refresh_Image_Free(pixels);
			NativeMemory.Free(copiedBytes);
		}

		public override void Update(System.TimeSpan delta) { }

		public override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
			if (swapchainTexture != null)
			{
				var clearPass = cmdbuf.BeginRenderPass(
					new ColorAttachmentInfo(swapchainTexture, false, Color.Black)
				);
				cmdbuf.EndRenderPass(clearPass);

				cmdbuf.Blit(
					OriginalTexture,
					new TextureRegion
					{
						TextureSlice = swapchainTexture,
						Width = swapchainTexture.Width / 2,
						Height = swapchainTexture.Height / 2,
						Depth = 1
					},
					Filter.Nearest,
					false
				);

				cmdbuf.Blit(
					TextureCopy,
					new TextureRegion
					{
						TextureSlice = swapchainTexture,
						X = swapchainTexture.Width / 2,
						Y = 0,
						Width = swapchainTexture.Width / 2,
						Height = swapchainTexture.Height / 2,
						Depth = 1
					},
					Filter.Nearest,
					false
				);

				cmdbuf.Blit(
					TextureSmall,
					new TextureRegion
					{
						TextureSlice = swapchainTexture,
						X = swapchainTexture.Width / 4,
						Y = swapchainTexture.Height / 2,
						Width = swapchainTexture.Width / 2,
						Height = swapchainTexture.Height / 2,
						Depth = 1
					},
					Filter.Nearest,
					false
				);
			}

			GraphicsDevice.Submit(cmdbuf);
		}

        public override void Destroy()
        {
            OriginalTexture.Dispose();
			TextureCopy.Dispose();
			TextureSmall.Dispose();
        }
    }
}
