using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Test;

namespace MoonWorksGraphicsTests;

class Program : Game
{
	Example[] Examples =
	[
		new BasicComputeExample(),
		new BasicStencilGame()
	];

	int ExampleIndex = 0;

    public Program(
		WindowCreateInfo windowCreateInfo,
		FrameLimiterSettings frameLimiterSettings,
		BackendFlags preferredBackends,
		int targetTimestep = 60,
		bool debugMode = false
	) : base(windowCreateInfo, frameLimiterSettings, preferredBackends, targetTimestep, debugMode)
    {
    }

    protected override void Update(TimeSpan delta)
    {
		if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.A))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex -= 1;
			if (ExampleIndex < 0)
			{
				ExampleIndex = Examples.Length - 1;
			}

			Examples[ExampleIndex].Init(MainWindow, GraphicsDevice);
		}
		else if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.D))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex = (ExampleIndex + 1) % Examples.Length;

			Examples[ExampleIndex].Init(MainWindow, GraphicsDevice);
		}
		else
		{
        	Examples[ExampleIndex].Update(delta);
		}
    }

    protected override void Draw(double alpha)
    {
        Examples[ExampleIndex].Draw(alpha);
    }

    static void Main(string[] args)
	{
		var debugMode = false;

		#if DEBUG
		debugMode = true;
		#endif

		var windowCreateInfo = new WindowCreateInfo(
			"MoonWorksGraphicsTests",
			1280,
			720,
			ScreenMode.Windowed,
			SwapchainComposition.SDR,
			PresentMode.VSync
		);

		var frameLimiterSettings = new FrameLimiterSettings(
			FrameLimiterMode.Capped,
			60
		);

		var game = new Program(
			windowCreateInfo,
			frameLimiterSettings,
			BackendFlags.Vulkan | BackendFlags.D3D11 | BackendFlags.Metal,
			60,
			debugMode
		);

		game.Run();
	}
}
