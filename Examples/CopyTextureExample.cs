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
				TestUtils.GetTexturePath("ravioli.png"),
				TextureFormat.R8G8B8A8Unorm,
				TextureUsageFlags.Sampler
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
				Type = OriginalTexture.Type,
				Format = OriginalTexture.Format,
				Usage = OriginalTexture.UsageFlags,
				Width = OriginalTexture.Width,
				Height = OriginalTexture.Height,
				LayerCountOrDepth = OriginalTexture.LayerCountOrDepth,
				NumLevels = OriginalTexture.LevelCount,
				SampleCount = OriginalTexture.SampleCount
			};

			// Create a 1:1 copy of the texture
			TextureCopy = Texture.Create(GraphicsDevice, textureCreateInfo);

			// Create a download transfer buffer
			TransferBuffer compareBuffer = TransferBuffer.Create<byte>(
				GraphicsDevice,
				TransferBufferUsage.Download,
				byteCount
			);

			var copyPass = cmdbuf.BeginCopyPass();
			copyPass.CopyTextureToTexture(
				new TextureLocation
				{
					Texture = OriginalTexture.Handle
				},
				new TextureLocation
				{
					Texture = TextureCopy.Handle
				},
				OriginalTexture.Width,
				OriginalTexture.Height,
				1,
				false
			);
			cmdbuf.EndCopyPass(copyPass);

			// Create a half-sized copy of this texture
			textureCreateInfo.Width /= 2;
			textureCreateInfo.Height /= 2;
			textureCreateInfo.Usage |= TextureUsageFlags.ColorTarget;
			TextureSmall = Texture.Create(GraphicsDevice, textureCreateInfo);

			// Render the half-size copy
			cmdbuf.Blit(new BlitInfo
			{
				LoadOp = LoadOp.DontCare,
				Source = new BlitRegion
				{
					Texture = OriginalTexture.Handle,
					W = OriginalTexture.Width,
					H = OriginalTexture.Height
				},
				Destination = new BlitRegion
				{
					Texture = TextureSmall.Handle,
					W = TextureSmall.Width,
					H = TextureSmall.Height
				},
				Filter = Filter.Linear
			});

			// Copy the texture to a transfer buffer
			copyPass = cmdbuf.BeginCopyPass();
			copyPass.DownloadFromTexture(
				new TextureRegion
				{
					Texture = TextureCopy.Handle,
					W = TextureCopy.Width,
					H = TextureCopy.Height,
					D = 1,
				},
				new TextureTransferInfo
				{
					TransferBuffer = compareBuffer.Handle
				}
			);
			cmdbuf.EndCopyPass(copyPass);

			var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFence(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Compare the original bytes to the copied bytes.
			var copiedSpan = compareBuffer.Map<byte>(false);
			var originalSpan = new System.Span<byte>(pixels, (int)byteCount);

			if (System.MemoryExtensions.SequenceEqual(originalSpan, copiedSpan))
			{
				Logger.LogInfo("SUCCESS! Original texture bytes and the downloaded bytes match!");

			}
			else
			{
				Logger.LogError("FAIL! Original texture bytes do not match downloaded bytes!");
			}
			compareBuffer.Unmap();

			ImageUtils.FreePixelData(pixels);
		}

		public override void Update(System.TimeSpan delta) { }

		public override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
			if (swapchainTexture != null)
			{
				var clearPass = cmdbuf.BeginRenderPass(
					new ColorTargetInfo
					{
						Texture = swapchainTexture.Handle,
						LoadOp = LoadOp.Clear,
						ClearColor = Color.Black
					}
				);
				cmdbuf.EndRenderPass(clearPass);

				cmdbuf.Blit(new BlitInfo
				{
					Source = new BlitRegion
					{
						Texture = OriginalTexture.Handle,
						W = OriginalTexture.Width,
						H = OriginalTexture.Height
					},
					Destination = new BlitRegion
					{
						Texture = swapchainTexture.Handle,
						W = swapchainTexture.Width / 2,
						H = swapchainTexture.Height / 2
					},
					Filter = Filter.Nearest
				});

				cmdbuf.Blit(new BlitInfo
				{
					Source = new BlitRegion
					{
						Texture = TextureCopy.Handle,
						W = TextureCopy.Width,
						H = TextureCopy.Height
					},
					Destination = new BlitRegion
					{
						Texture = swapchainTexture.Handle,
						X = swapchainTexture.Width / 2,
						W = swapchainTexture.Width / 2,
						H = swapchainTexture.Height / 2,
					},
					Filter = Filter.Nearest
				});

				cmdbuf.Blit(new BlitInfo
				{
					Source = new BlitRegion
					{
						Texture = TextureSmall.Handle,
						W = TextureSmall.Width,
						H = TextureSmall.Height
					},
					Destination = new BlitRegion
					{
						Texture = swapchainTexture.Handle,
						X = swapchainTexture.Width / 4,
						Y = swapchainTexture.Height / 2,
						W = swapchainTexture.Width / 2,
						H = swapchainTexture.Height / 2
					},
					Filter = Filter.Nearest
				});
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
