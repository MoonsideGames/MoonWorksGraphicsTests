using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using RefreshCS;

namespace MoonWorks.Test
{
    class Texture3DGame : Game
    {
        private GraphicsPipeline pipeline;
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Texture texture;
        private Sampler sampler;

        private int currentDepth = 0;

        struct FragUniform
        {
            public float Depth;

            public FragUniform(float depth)
            {
                Depth = depth;
            }
        }

        public Texture3DGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            Logger.LogInfo("Press Left and Right to cycle between depth slices");

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
            sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

            // Create and populate the GPU resources
            vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
            indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);
            texture = Texture.CreateTexture3D(GraphicsDevice, 16, 16, 7, TextureFormat.R8G8B8A8, TextureUsageFlags.Sampler);

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

            // Load each depth subimage of the 3D texture
            for (uint i = 0; i < texture.Depth; i += 1)
            {
                TextureSlice slice = new TextureSlice(
                    texture,
                    new Rect(0, 0, (int) texture.Width, (int) texture.Height),
                    i
                );

				Texture.SetDataFromImageFile(
					cmdbuf,
					slice,
					TestUtils.GetTexturePath($"tex3d_{i}.png")
				);
            }

            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Update(System.TimeSpan delta)
        {
            int prevDepth = currentDepth;

            if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
            {
                currentDepth -= 1;
                if (currentDepth < 0)
                {
                    currentDepth = (int) texture.Depth - 1;
                }
            }

            if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
            {
                currentDepth += 1;
                if (currentDepth >= texture.Depth)
                {
                    currentDepth = 0;
                }
            }

            if (prevDepth != currentDepth)
            {
                Logger.LogInfo("Setting depth to: " + currentDepth);
            }
        }

        protected override void Draw(double alpha)
        {
            FragUniform fragUniform = new FragUniform((float) currentDepth / texture.Depth);

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
                cmdbuf.BindGraphicsPipeline(pipeline);
                cmdbuf.BindVertexBuffers(vertexBuffer);
                cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
                cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
                uint fragParamOffset = cmdbuf.PushFragmentShaderUniforms(fragUniform);
                cmdbuf.DrawIndexedPrimitives(0, 0, 2, 0, fragParamOffset);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            Texture3DGame game = new Texture3DGame();
            game.Run();
        }
    }
}
