using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
    class DrawIndirectGame : Game
    {
        private GraphicsPipeline graphicsPipeline;
        private Buffer vertexBuffer;
        private Buffer drawBuffer;

        public DrawIndirectGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            // Load the shaders
            ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("PositionColorVert.spv"));
            ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("SolidColor.spv"));

            // Create the graphics pipeline
            GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
                MainWindow.SwapchainFormat,
                vertShaderModule,
                fragShaderModule
            );
            pipelineCreateInfo.VertexInputState = new VertexInputState(
                VertexBinding.Create<PositionColorVertex>(),
                VertexAttribute.Create<PositionColorVertex>("Position", 0),
                VertexAttribute.Create<PositionColorVertex>("Color", 1)
            );
            graphicsPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

            // Create and populate the vertex buffer
            vertexBuffer = Buffer.Create<PositionColorVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 6);
            drawBuffer = Buffer.Create<DrawIndirectCommand>(GraphicsDevice, BufferUsageFlags.Indirect, 2);

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(
                vertexBuffer,
                new PositionColorVertex[]
                {
                    new PositionColorVertex(new Vector3(-0.5f, -1, 0), Color.Blue),
                    new PositionColorVertex(new Vector3(-1f, 1, 0), Color.Green),
                    new PositionColorVertex(new Vector3(0f, 1, 0), Color.Red),

                    new PositionColorVertex(new Vector3(.5f, -1, 0), Color.Blue),
                    new PositionColorVertex(new Vector3(1f, 1, 0), Color.Green),
                    new PositionColorVertex(new Vector3(0f, 1, 0), Color.Red),
                }
            );
            cmdbuf.SetBufferData(
                drawBuffer,
                new DrawIndirectCommand[]
                {
                    new DrawIndirectCommand(3, 1, 3, 0),
                    new DrawIndirectCommand(3, 1, 0, 0),
                }
            );
            GraphicsDevice.Submit(cmdbuf);
            GraphicsDevice.Wait();
        }

        protected override void Update(System.TimeSpan delta) { }

        protected override void Draw(double alpha)
        {
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.CornflowerBlue));
                cmdbuf.BindGraphicsPipeline(graphicsPipeline);
                cmdbuf.BindVertexBuffers(new BufferBinding(vertexBuffer, 0));
                cmdbuf.DrawPrimitivesIndirect(drawBuffer, 0, 2, (uint) Marshal.SizeOf<DrawIndirectCommand>(), 0, 0);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            DrawIndirectGame game = new DrawIndirectGame();
            game.Run();
        }
    }
}
