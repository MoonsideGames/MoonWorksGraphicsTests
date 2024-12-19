using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using System.Numerics;

namespace MoonWorksGraphicsTests;

class FontExample : Example
{
	Font SofiaSans;
	TextBatch TextBatch;
	GraphicsPipeline FontPipeline;

	float rotation;

	public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;
		Inputs = inputs;

		Window.SetTitle("Font");

		SofiaSans = Font.Load(GraphicsDevice, TestUtils.GetFontPath("SofiaSans.ttf"));
		TextBatch = new TextBatch(GraphicsDevice);

		var fontPipelineCreateInfo = new GraphicsPipelineCreateInfo
		{
			VertexShader = GraphicsDevice.TextVertexShader,
			FragmentShader = GraphicsDevice.TextFragmentShader,
			VertexInputState = GraphicsDevice.TextVertexInputState,
			PrimitiveType = PrimitiveType.TriangleList,
			RasterizerState = RasterizerState.CCW_CullNone,
			MultisampleState = MultisampleState.None,
			DepthStencilState = DepthStencilState.Disable,
			TargetInfo = new GraphicsPipelineTargetInfo
			{
				ColorTargetDescriptions = [
					new ColorTargetDescription
					{
						Format = Window.SwapchainFormat,
						BlendState = ColorTargetBlendState.Opaque
					}
				]
			}
		};

		FontPipeline = GraphicsPipeline.Create(GraphicsDevice, fontPipelineCreateInfo);

		Logger.LogInfo("Press Left and Right to rotate the text!");
    }

    public override void Update(TimeSpan delta)
    {
		if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Left))
		{
			rotation -= (float) delta.TotalSeconds;
		}
		else if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Right))
		{
			rotation += (float) delta.TotalSeconds;
		}
    }

	public override void Draw(double alpha)
    {
		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);

		if (swapchainTexture != null)
		{
			Matrix4x4 proj = Matrix4x4.CreateOrthographicOffCenter(
				0,
				640,
				480,
				0,
				0,
				-1
			);

			Matrix4x4 model =
				Matrix4x4.CreateRotationX(rotation) *
				Matrix4x4.CreateTranslation(320, 240, 0);

			TextBatch.Start(SofiaSans);
			TextBatch.Add(
				"THIS IS SOME TEXT.",
				64,
				Color.White,
				HorizontalAlignment.Center,
				VerticalAlignment.Middle
			);
			TextBatch.UploadBufferData(cmdbuf);

			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Black)
			);

			renderPass.BindGraphicsPipeline(FontPipeline);
			TextBatch.Render(renderPass, model * proj);
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
    }

    public override void Destroy()
    {
		TextBatch.Dispose();
		SofiaSans.Dispose();
    }
}
