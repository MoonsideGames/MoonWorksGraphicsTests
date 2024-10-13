using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class StoreLoadExample : Example
{
	private GraphicsPipeline FillPipeline;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("StoreLoad");

		Shader vertShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("RawTriangle.vert"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Vertex,
				Format = ShaderFormat.SPIRV
			}
		);

		Shader fragShader = Shader.Create(
			GraphicsDevice,
			TestUtils.GetShaderPath("SolidColor.frag"),
			"main",
			new ShaderCreateInfo
			{
				Stage = ShaderStage.Fragment,
				Format = ShaderFormat.SPIRV
			}
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
				new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Blue
				}
			);
			renderPass.BindGraphicsPipeline(FillPipeline);
			renderPass.DrawPrimitives(3, 1, 0, 0);
			cmdbuf.EndRenderPass(renderPass);

			renderPass = cmdbuf.BeginRenderPass(
				new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Load,
					StoreOp = StoreOp.Store
				}
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
