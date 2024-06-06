using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class RenderTexture2DExample : Example
{
	private Texture[] textures = new Texture[4];

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("RenderTexture2D");

		for (int i = 0; i < textures.Length; i += 1)
		{
			textures[i] = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width / 4,
				Window.Height / 4,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
			);
		}
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[0], false, Color.Red));
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[1], false, Color.Blue));
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[2], false, Color.Green));
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(new ColorAttachmentInfo(textures[3], false, Color.Yellow));
			cmdbuf.EndRenderPass(renderPass);

			cmdbuf.Blit(
				textures[0],
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
				textures[1],
				new TextureRegion
				{
					TextureSlice = swapchainTexture,
					X = swapchainTexture.Width / 2,
					Width = swapchainTexture.Width / 2,
					Height = swapchainTexture.Height / 2,
					Depth = 1
				},
				Filter.Nearest,
				false
			);

			cmdbuf.Blit(
				textures[2],
				new TextureRegion
				{
					TextureSlice = swapchainTexture,
					Y = swapchainTexture.Height / 2,
					Width = swapchainTexture.Width / 2,
					Height = swapchainTexture.Height / 2,
					Depth = 1
				},
				Filter.Nearest,
				false
			);

			cmdbuf.Blit(
				textures[3],
				new TextureRegion
				{
					TextureSlice = swapchainTexture,
					X = swapchainTexture.Width / 2,
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
        for (var i = 0; i < 4; i += 1)
		{
			textures[i].Dispose();
		}
    }
}
