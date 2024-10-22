using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class RenderTexture2DArrayExample : Example
{
	private Texture RenderTarget;

	private float t;
	private Color[] colors =
    [
        Color.Red,
		Color.Green,
		Color.Blue,
	];

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("RenderTexture2DArray");

		RenderTarget = Texture.Create2DArray(
			GraphicsDevice,
			16,
			16,
			(uint) colors.Length,
			TextureFormat.R8G8B8A8Unorm,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each depth slice of the RT to a different color
		for (uint i = 0; i < colors.Length; i += 1)
		{
            var renderPass = cmdbuf.BeginRenderPass(new ColorTargetInfo
            {
                Texture = RenderTarget.Handle,
                LayerOrDepthPlane = i,
                LoadOp = LoadOp.Clear,
                ClearColor = colors[i],
                StoreOp = StoreOp.Store
            });
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(System.TimeSpan delta)
	{
		t += (float) delta.TotalSeconds;
		t %= 3;
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			cmdbuf.Blit(new BlitInfo
			{
				Source = new BlitRegion
				{
					Texture = RenderTarget.Handle,
					LayerOrDepthPlane = (uint) Math.Floor(t),
					W = RenderTarget.Width,
					H = RenderTarget.Height
				},
				Destination = new BlitRegion(swapchainTexture),
				Filter = Filter.Nearest,
				LoadOp = LoadOp.DontCare
			});
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        RenderTarget.Dispose();
    }
}
