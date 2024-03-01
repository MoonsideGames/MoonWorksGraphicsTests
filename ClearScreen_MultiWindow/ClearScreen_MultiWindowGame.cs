using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class ClearScreen_MultiWindowGame : Game
	{
		private Window secondaryWindow;

		public ClearScreen_MultiWindowGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			secondaryWindow = new Window(
				new WindowCreateInfo("Secondary Window", 640, 480, ScreenMode.Windowed, PresentMode.FIFO, false, false),
				GraphicsDevice.WindowFlags
			);
			GraphicsDevice.ClaimWindow(secondaryWindow, PresentMode.FIFO);
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf;
			Texture? backbuffer;

			if (MainWindow.Claimed)
			{
				cmdbuf = GraphicsDevice.AcquireCommandBuffer();
				backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
				if (backbuffer != null)
				{
					cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.CornflowerBlue));
					cmdbuf.EndRenderPass();
				}
				GraphicsDevice.Submit(cmdbuf);
			}

			if (secondaryWindow.Claimed)
			{
				cmdbuf = GraphicsDevice.AcquireCommandBuffer();
				backbuffer = cmdbuf.AcquireSwapchainTexture(secondaryWindow);
				if (backbuffer != null)
				{
					cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.Aquamarine));
					cmdbuf.EndRenderPass();
				}
				GraphicsDevice.Submit(cmdbuf);
			}
		}

		public static void Main(string[] args)
		{
			ClearScreen_MultiWindowGame game = new ClearScreen_MultiWindowGame();
			game.Run();
		}
	}
}
