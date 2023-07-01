using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using RefreshCS;

namespace MoonWorks.Test
{
    class RenderTexture3DGame : Game
    {
        private GraphicsPipeline pipeline;
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Texture rt;
        private Sampler sampler;

        private float t;
        private Color[] colors = new Color[]
        {
            Color.Red,
            Color.Green,
            Color.Blue,
        };

        struct FragUniform
        {
            public float Depth;

            public FragUniform(float depth)
            {
                Depth = depth;
            }
        }

        public RenderTexture3DGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            // Load the shaders
            ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
            ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad3D.frag"));

            // Create the graphics pipeline
            GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
                MainWindow.SwapchainFormat,
                vertShaderModule,
                fragShaderModule
            );
            pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
            pipelineCreateInfo.FragmentShaderInfo = GraphicsShaderInfo.Create<FragUniform>(fragShaderModule, "main", 1);
            pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

            // Create samplers
            sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearWrap);

            // Create and populate the GPU resources
            vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
            indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);
            rt = Texture.CreateTexture3D(
                GraphicsDevice,
                16,
                16,
                (uint) colors.Length,
                TextureFormat.R8G8B8A8,
                TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
            );

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(
                vertexBuffer,
                new PositionTextureVertex[]
                {
                    new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                    new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
                    new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
                    new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
                }
            );
            cmdbuf.SetBufferData(
                indexBuffer,
                new ushort[]
                {
                    0, 1, 2,
                    0, 2, 3,
                }
            );

            // Clear each depth slice of the RT to a different color
            for (uint i = 0; i < colors.Length; i += 1)
            {
                ColorAttachmentInfo attachmentInfo = new ColorAttachmentInfo
                {
                    Texture = rt,
                    ClearColor = colors[i],
                    Depth = i,
                    Layer = 0,
                    Level = 0,
                    LoadOp = LoadOp.Clear,
                    StoreOp = StoreOp.Store
                };
                cmdbuf.BeginRenderPass(attachmentInfo);
                cmdbuf.EndRenderPass();
            }

            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Update(System.TimeSpan delta) { }

        protected override void Draw(double alpha)
        {
            t += 0.01f;
            FragUniform fragUniform = new FragUniform(t);

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
                cmdbuf.BindGraphicsPipeline(pipeline);
                cmdbuf.BindVertexBuffers(vertexBuffer);
                cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
                cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(rt, sampler));
                uint fragParamOffset = cmdbuf.PushFragmentShaderUniforms(fragUniform);
                cmdbuf.DrawIndexedPrimitives(0, 0, 2, 0, fragParamOffset);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            RenderTexture3DGame game = new RenderTexture3DGame();
            game.Run();
        }
    }
}
