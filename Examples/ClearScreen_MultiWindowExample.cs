using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using SDL3;

namespace MoonWorksGraphicsTests
{
	class ClearScreen_MultiWindowExample : Example
	{
		private Window SecondaryWindow;

        public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
		{
			Window = window;
			GraphicsDevice = graphicsDevice;

			Window.SetTitle("ClearScreen");
			var (windowX, windowY) = Window.Position;
			Window.SetPosition(windowX - 360, windowY);

			SecondaryWindow = new Window(
				new WindowCreateInfo("Secondary Window", 640, 480, ScreenMode.Windowed, false, false),
				0
			);
			(windowX, windowY) = SecondaryWindow.Position;
			SecondaryWindow.SetPosition(windowX + 360, windowY);

			GraphicsDevice.ClaimWindow(SecondaryWindow);
		}

		public override void Update(System.TimeSpan delta) { }

		public override void Draw(double alpha)
		{
			CommandBuffer cmdbuf;
			Texture swapchainTexture;

			if (Window.Claimed)
			{
				cmdbuf = GraphicsDevice.AcquireCommandBuffer();
				swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
				if (swapchainTexture != null)
				{
					var renderPass = cmdbuf.BeginRenderPass(
						new ColorTargetInfo
						{
							Texture = swapchainTexture.Handle,
							LoadOp = LoadOp.Clear,
							ClearColor = Color.CornflowerBlue
						}
					);
					cmdbuf.EndRenderPass(renderPass);
				}
				GraphicsDevice.Submit(cmdbuf);
			}

			if (SecondaryWindow.Claimed)
			{
				cmdbuf = GraphicsDevice.AcquireCommandBuffer();
				swapchainTexture = cmdbuf.AcquireSwapchainTexture(SecondaryWindow);
				if (swapchainTexture != null)
				{
					var renderPass = cmdbuf.BeginRenderPass(
						new ColorTargetInfo
						{
							Texture = swapchainTexture.Handle,
							LoadOp = LoadOp.Clear,
							ClearColor = Color.Aquamarine
						}
					);
					cmdbuf.EndRenderPass(renderPass);
				}
				GraphicsDevice.Submit(cmdbuf);
			}
		}

        public override void Destroy()
        {
			GraphicsDevice.UnclaimWindow(SecondaryWindow);
			SecondaryWindow.Dispose();

			Window.SetPositionCentered();
        }
    }
}
