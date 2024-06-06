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

		RenderTarget = Texture.CreateTexture2DArray(
			GraphicsDevice,
			16,
			16,
			(uint) colors.Length,
			TextureFormat.R8G8B8A8,
			TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
		);

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

		// Clear each depth slice of the RT to a different color
		for (uint i = 0; i < colors.Length; i += 1)
		{
			ColorAttachmentInfo attachmentInfo = new ColorAttachmentInfo
			{
				TextureSlice = new TextureSlice
				{
					Texture = RenderTarget,
					Layer = i,
					MipLevel = 0
				},
				ClearColor = colors[i],
				LoadOp = LoadOp.Clear,
				StoreOp = StoreOp.Store
			};

			var renderPass = cmdbuf.BeginRenderPass(attachmentInfo);
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		t += 0.01f;
		t %= 3;

		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			cmdbuf.Blit(
				new TextureRegion
				{
					TextureSlice = new TextureSlice
					{
						Texture = RenderTarget,
						Layer = (uint) MathF.Floor(t)
					},
					Depth = 1
				},
				swapchainTexture,
				Filter.Nearest,
				false
			);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        RenderTarget.Dispose();
    }
}
