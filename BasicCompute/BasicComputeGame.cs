using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
    class BasicComputeGame : Game
    {
        private GraphicsPipeline drawPipeline;
        private Texture texture;
        private Sampler sampler;
        private Buffer vertexBuffer;

        public BasicComputeGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            // Create the compute pipeline that writes texture data
            ShaderModule fillTextureComputeShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("FillTextureCompute.spv")
            );

            ComputePipeline fillTextureComputePipeline = new ComputePipeline(
                GraphicsDevice,
                ComputeShaderInfo.Create(fillTextureComputeShaderModule, "main", 0, 1)
            );

            // Create the compute pipeline that calculates squares of numbers
            ShaderModule calculateSquaresComputeShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("CalculateSquaresCompute.spv")
            );

            ComputePipeline calculateSquaresComputePipeline = new ComputePipeline(
                GraphicsDevice,
                ComputeShaderInfo.Create(calculateSquaresComputeShaderModule, "main", 1, 0)
            );

            // Create the graphics pipeline
            ShaderModule vertShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("TexturedQuadVert.spv")
            );

            ShaderModule fragShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("TexturedQuadFrag.spv")
            );

            GraphicsPipelineCreateInfo drawPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
                vertShaderModule,
                fragShaderModule
            );
            drawPipelineCreateInfo.VertexInputState = new VertexInputState(
                VertexBinding.Create<PositionTextureVertex>(),
                VertexAttribute.Create<PositionTextureVertex>("Position", 0),
                VertexAttribute.Create<PositionTextureVertex>("TexCoord", 1)
            );
            drawPipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;

            drawPipeline = new GraphicsPipeline(
                GraphicsDevice,
                drawPipelineCreateInfo
            );

            // Create buffers and textures
            float[] squares = new float[64];
            Buffer squaresBuffer = Buffer.Create<float>(
                GraphicsDevice,
                BufferUsageFlags.Compute,
                (uint) squares.Length
            );

            vertexBuffer = Buffer.Create<PositionTextureVertex>(
                GraphicsDevice,
                BufferUsageFlags.Vertex,
                6
            );

            texture = Texture.CreateTexture2D(
                GraphicsDevice,
                MainWindow.Width,
                MainWindow.Height,
                TextureFormat.R8G8B8A8,
                TextureUsageFlags.Compute | TextureUsageFlags.Sampler
            );

            sampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

            // Upload GPU resources and dispatch compute work
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(vertexBuffer, new PositionTextureVertex[]
            {
                new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
                new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
                new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
                new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
            });

            cmdbuf.BindComputePipeline(fillTextureComputePipeline);
            cmdbuf.BindComputeTextures(texture);
            cmdbuf.DispatchCompute(MainWindow.Width / 8, MainWindow.Height / 8, 1, 0);

            cmdbuf.BindComputePipeline(calculateSquaresComputePipeline);
            cmdbuf.BindComputeBuffers(squaresBuffer);
            cmdbuf.DispatchCompute(64, 1, 1, 0);

            GraphicsDevice.Submit(cmdbuf);
            GraphicsDevice.Wait();

            squaresBuffer.GetData(squares, (uint) (sizeof(float) * squares.Length));
            Logger.LogInfo("Squares of the first " + squares.Length + " integers: " + string.Join(", ", squares));
        }

        protected override void Update(System.TimeSpan delta) { }

        protected override void Draw(double alpha)
        {
            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.CornflowerBlue));
                cmdbuf.BindGraphicsPipeline(drawPipeline);
                cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
                cmdbuf.BindVertexBuffers(vertexBuffer);
                cmdbuf.DrawPrimitives(0, 2, 0, 0);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            BasicComputeGame game = new BasicComputeGame();
            game.Run();
        }
    }
}
