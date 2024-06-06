using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

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
			Window.SetPosition(SDL2.SDL.SDL_WINDOWPOS_CENTERED, SDL2.SDL.SDL_WINDOWPOS_CENTERED);
			var (windowX, windowY) = Window.Position;
			Window.SetPosition(windowX - 360, windowY);

			SecondaryWindow = new Window(
				new WindowCreateInfo("Secondary Window", 640, 480, ScreenMode.Windowed, SwapchainComposition.SDR, PresentMode.VSync, false, false),
				SDL2.SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN
			);
			(windowX, windowY) = SecondaryWindow.Position;
			SecondaryWindow.SetPosition(windowX + 360, windowY);

			GraphicsDevice.ClaimWindow(SecondaryWindow, SwapchainComposition.SDR, PresentMode.VSync);
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
						new ColorAttachmentInfo(swapchainTexture, false, Color.CornflowerBlue)
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
						new ColorAttachmentInfo(swapchainTexture, false, Color.Aquamarine)
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

			Window.SetPosition(SDL2.SDL.SDL_WINDOWPOS_CENTERED, SDL2.SDL.SDL_WINDOWPOS_CENTERED);
        }
    }
}
