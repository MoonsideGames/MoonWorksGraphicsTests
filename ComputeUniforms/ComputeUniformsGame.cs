using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
    class ComputeUniformsGame : Game
    {
        private GraphicsPipeline drawPipeline;
        private Texture texture;
        private Sampler sampler;
        private Buffer vertexBuffer;

        struct GradientTextureComputeUniforms
        {
            public uint groupCountX;
            public uint groupCountY;

            public GradientTextureComputeUniforms(uint w, uint h)
            {
                groupCountX = w;
                groupCountY = h;
            }
        }

        public ComputeUniformsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            // Create the compute pipeline that writes texture data
            ShaderModule gradientTextureComputeShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("GradientTextureCompute.spv")
            );

            ComputePipeline gradientTextureComputePipeline = new ComputePipeline(
                GraphicsDevice,
                ComputeShaderInfo.Create<GradientTextureComputeUniforms>(gradientTextureComputeShaderModule, "main", 0, 1)
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
            drawPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
            drawPipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;

            drawPipeline = new GraphicsPipeline(
                GraphicsDevice,
                drawPipelineCreateInfo
            );

            // Create buffers and textures
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

            GradientTextureComputeUniforms gradientUniforms = new GradientTextureComputeUniforms(
                texture.Width / 8,
                texture.Height / 8
            );

            cmdbuf.BindComputePipeline(gradientTextureComputePipeline);
            cmdbuf.BindComputeTextures(texture);
            uint offset = cmdbuf.PushComputeShaderUniforms(gradientUniforms);
            cmdbuf.DispatchCompute(gradientUniforms.groupCountX, gradientUniforms.groupCountY, 1, offset);

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
            ComputeUniformsGame game = new ComputeUniformsGame();
            game.Run();
        }
    }
}
