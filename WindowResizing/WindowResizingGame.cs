using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorks.Test
{
    class WindowResizingGame : Game
    {
        private GraphicsPipeline pipeline;

        private int currentResolutionIndex;
        private record struct Res(uint Width, uint Height);
        private Res[] resolutions = new Res[]
        {
            new Res(640, 480),
            new Res(1280, 720),
            new Res(1024, 1024),
            new Res(1600, 900),
            new Res(1920, 1080),
            new Res(3200, 1800),
            new Res(3840, 2160),
        };

        public WindowResizingGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("RawTriangle.vert"));
            ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.frag"));

            GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
                MainWindow.SwapchainFormat,
                vertShaderModule,
                fragShaderModule
            );
            pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);
        }

        protected override void Update(System.TimeSpan delta)
        {
            int prevResolutionIndex = currentResolutionIndex;

            if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
            {
                currentResolutionIndex -= 1;
                if (currentResolutionIndex < 0)
                {
                    currentResolutionIndex = resolutions.Length - 1;
                }
            }

            if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
            {
                currentResolutionIndex += 1;
                if (currentResolutionIndex >= resolutions.Length)
                {
                    currentResolutionIndex = 0;
                }
            }

            if (prevResolutionIndex != currentResolutionIndex)
            {
                Logger.LogInfo("Setting resolution to: " + resolutions[currentResolutionIndex]);
                MainWindow.SetWindowSize(resolutions[currentResolutionIndex].Width, resolutions[currentResolutionIndex].Height);
            }
        }

        protected override void Draw(double alpha)
        {
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
                cmdbuf.BindGraphicsPipeline(pipeline);
                cmdbuf.DrawPrimitives(0, 1, 0, 0);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            WindowResizingGame game = new WindowResizingGame();
            game.Run();
        }
    }
}
