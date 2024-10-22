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
			textures[i] = Texture.Create2D(
				GraphicsDevice,
				Window.Width / 4,
				Window.Height / 4,
				TextureFormat.R8G8B8A8Unorm,
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
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(textures[0], Color.Red)
			);
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(textures[1], Color.DodgerBlue)
			);
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(textures[2], Color.Green)
			);
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(textures[3], Color.DarkGoldenrod)
			);
			cmdbuf.EndRenderPass(renderPass);

			cmdbuf.Blit(new BlitInfo
			{
				Source = new BlitRegion(textures[0]),
				Destination = new BlitRegion
				{
					Texture = swapchainTexture.Handle,
					W = swapchainTexture.Width / 2,
					H = swapchainTexture.Height / 2,
				},
				Filter = Filter.Nearest
			});

			cmdbuf.Blit(new BlitInfo
			{
				Source = new BlitRegion(textures[1]),
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
				Source = new BlitRegion(textures[2]),
				Destination = new BlitRegion
				{
					Texture = swapchainTexture.Handle,
					Y = swapchainTexture.Height / 2,
					W = swapchainTexture.Width / 2,
					H = swapchainTexture.Height / 2,
				},
				Filter = Filter.Nearest
			});

			cmdbuf.Blit(new BlitInfo
			{
				Source = new BlitRegion(textures[3]),
				Destination = new BlitRegion
				{
					Texture = swapchainTexture.Handle,
					X = swapchainTexture.Width / 2,
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
        for (var i = 0; i < 4; i += 1)
		{
			textures[i].Dispose();
		}
    }
}
