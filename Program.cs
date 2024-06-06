using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorksGraphicsTests;

class Program : Game
{
	Example[] Examples =
	[
		new ClearScreenExample(),
		new ClearScreen_MultiWindowExample(),
		new BasicStencilExample(),
		new BasicTriangleExample(),
		new CompressedTexturesExample(),
		new BasicComputeExample(),
		new ComputeUniformsExample(),
		new CopyTextureExample(),
		new CubeExample(),
		new CullFaceExample(),
		new DepthMSAAExample(),
		new DrawIndirectExample(),
		new GetBufferDataExample(),
		new InstancingAndOffsetsExample(),
		new MSAACubeExample(),
		new MSAAExample(),
		new RenderTexture2DArrayExample(),
		new RenderTexture2DExample(),
		new RenderTextureCubeExample()
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
		Logger.LogInfo("Welcome to the MoonWorks Graphics Tests program! Press Q and E to cycle through examples!");
		Examples[ExampleIndex].Init(MainWindow, GraphicsDevice, Inputs);
    }

    protected override void Update(TimeSpan delta)
    {
		if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.Q))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex -= 1;
			if (ExampleIndex < 0)
			{
				ExampleIndex = Examples.Length - 1;
			}

			Examples[ExampleIndex].Init(MainWindow, GraphicsDevice, Inputs);
		}
		else if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.E))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex = (ExampleIndex + 1) % Examples.Length;

			Examples[ExampleIndex].Init(MainWindow, GraphicsDevice, Inputs);
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

    protected override void Destroy()
    {
        Examples[ExampleIndex].Destroy();
    }

    static void Main(string[] args)
	{
		var debugMode = false;

		#if DEBUG
		debugMode = true;
		#endif

		var windowCreateInfo = new WindowCreateInfo(
			"MoonWorksGraphicsTests",
			640,
			480,
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
