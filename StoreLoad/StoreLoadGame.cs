using System;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
	class StoreLoadGame : Game
	{
		private GraphicsPipeline fillPipeline;

		public StoreLoadGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), TestUtils.DefaultBackend, 60, true)
		{
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangle.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			fillPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
		}

		protected override void Update(TimeSpan delta)
		{

		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (swapchain != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchain, WriteOptions.SafeDiscard, Color.Blue));
				cmdbuf.BindGraphicsPipeline(fillPipeline);
				cmdbuf.DrawPrimitives(0, 1);
				cmdbuf.EndRenderPass();
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchain, WriteOptions.SafeOverwrite, LoadOp.Load, StoreOp.Store));
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			StoreLoadGame game = new StoreLoadGame();
			game.Run();
		}
	}
}
