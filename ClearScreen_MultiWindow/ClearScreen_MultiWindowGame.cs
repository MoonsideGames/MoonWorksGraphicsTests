using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class ClearScreen_MultiWindowGame : Game
	{
		private Window secondaryWindow;

		public ClearScreen_MultiWindowGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.PreferredBackends, 60, true)
		{
			var (windowX, windowY) = MainWindow.Position;
			MainWindow.SetPosition(windowX - 360, windowY);

			secondaryWindow = new Window(
				new WindowCreateInfo("Secondary Window", 640, 480, ScreenMode.Windowed, PresentMode.FIFO, false, false),
				GraphicsDevice.WindowFlags
			);
			(windowX, windowY) = secondaryWindow.Position;
			secondaryWindow.SetPosition(windowX + 360, windowY);
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
					cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.Cycle, Color.CornflowerBlue));
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
					cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.Cycle, Color.Aquamarine));
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
