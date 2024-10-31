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
		new FontExample(),
		new BasicComputeExample(),
		new ComputeUniformsExample(),
		new CopyTextureExample(),
		new CullFaceExample(),
		new DrawIndirectExample(),
		new GetBufferDataExample(),
		new InstancingAndOffsetsExample(),
		new MSAAExample(),
		new DepthMSAAExample(),
		new RenderTexture2DArrayExample(),
		new RenderTexture2DExample(),
		new RenderTextureCubeExample(),
		new RenderTextureMipmapsExample(),
		new StoreLoadExample(),
		new Texture3DCopyExample(),
		new Texture3DExample(),
		new TexturedQuadExample(),
		new TexturedAnimatedQuadExample(),
		new TextureMipmapsExample(),
		new TriangleVertexBufferExample(),
		new VertexSamplerExample(),
		new VideoPlayerExample(),
		new WindowResizingExample(),
		new CPUSpriteBatchExample(),
		new ComputeSpriteBatchExample(),
		new CubeExample(),
		new HotReloadShaderExample()
	];

	int ExampleIndex = 0;

    public Program(
		WindowCreateInfo windowCreateInfo,
		FrameLimiterSettings frameLimiterSettings,
		ShaderFormat availableShaderFormats,
		int targetTimestep = 60,
		bool debugMode = false
	) : base(
		windowCreateInfo,
		frameLimiterSettings,
		availableShaderFormats,
		targetTimestep,
		debugMode
	) {
		Logger.LogInfo("Welcome to the MoonWorks Graphics Tests program! Press Q and E to cycle through examples!");
		ShaderCross.Initialize();
		Examples[ExampleIndex].Init(MainWindow, GraphicsDevice, Inputs);
    }

    protected override void Update(TimeSpan delta)
    {
		if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Previous))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex -= 1;
			if (ExampleIndex < 0)
			{
				ExampleIndex = Examples.Length - 1;
			}

			MainWindow.SetSize(640, 480);
			MainWindow.SetPositionCentered();
			Examples[ExampleIndex].Init(MainWindow, GraphicsDevice, Inputs);
		}
		else if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Next))
		{
			Examples[ExampleIndex].Destroy();

			ExampleIndex = (ExampleIndex + 1) % Examples.Length;

			MainWindow.SetSize(640, 480);
			MainWindow.SetPositionCentered();
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
			ScreenMode.Windowed
		);

		var frameLimiterSettings = new FrameLimiterSettings(
			FrameLimiterMode.Capped,
			144
		);

		if (!ShaderCross.Initialize())
		{
			return;
		}

		var game = new Program(
			windowCreateInfo,
			frameLimiterSettings,
			ShaderCross.HLSLDestinationFormats,
			60,
			debugMode
		);

		game.Run();
	}
}
