using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorksGraphicsTests;

class StoreLoadExample : Example
{
	private GraphicsPipeline FillPipeline;

    public override void Init()
	{
		Window.SetTitle("StoreLoad");

		Shader vertShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("RawTriangle.vert"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Vertex
		);

		Shader fragShader = ShaderCross.Create(
			GraphicsDevice,
			TestUtils.GetHLSLPath("SolidColor.frag"),
			"main",
			ShaderCross.ShaderFormat.HLSL,
			ShaderStage.Fragment
		);

		GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
			Window.SwapchainFormat,
			vertShader,
			fragShader
		);
		FillPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);
	}

	public override void Update(TimeSpan delta)
	{

	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, Color.Blue)
			);
			renderPass.BindGraphicsPipeline(FillPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo(swapchainTexture, LoadOp.Load)
			);
			cmdbuf.EndRenderPass(renderPass);
		}

		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
        FillPipeline.Dispose();
    }
}
