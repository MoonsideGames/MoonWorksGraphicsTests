using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using RefreshCS;

namespace MoonWorks.Test
{
    class TextureMipmapsGame : Game
    {
        private GraphicsPipeline pipeline;
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Texture texture;
        private Sampler sampler;

        private float scale = 0.5f;

        private struct VertexUniforms
        {
            public Matrix4x4 TransformMatrix;

            public VertexUniforms(Matrix4x4 transformMatrix)
            {
                TransformMatrix = transformMatrix;
            }
        }

        public TextureMipmapsGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            Logger.LogInfo("Press Left and Right to shrink/expand the scale of the quad");

            // Load the shaders
            ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadVertWithMatrix"));
            ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuadFrag"));

            // Create the graphics pipeline
            GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
                MainWindow.SwapchainFormat,
                vertShaderModule,
                fragShaderModule
            );
            pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
            pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create<VertexUniforms>(vertShaderModule, "main", 0);
            pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
            pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

            // Create and populate the GPU resources
            sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);
            vertexBuffer = Buffer.Create<PositionTextureVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
            indexBuffer = Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);
            texture = Texture.CreateTexture2D(
                GraphicsDevice,
                256,
                256,
                TextureFormat.R8G8B8A8,
                TextureUsageFlags.Sampler,
                4
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

            // Set the various mip levels
            for (int i = 0; i < texture.LevelCount; i += 1)
            {
                int w = (int) texture.Width >> i;
                int h = (int) texture.Height >> i;
                TextureSlice slice = new TextureSlice(
                    texture,
                    new Rect(0, 0, w, h),
                    0,
                    0,
                    (uint) i
                );

                var pixels = Refresh.Refresh_Image_Load(
                    TestUtils.GetTexturePath($"mip{i}.png"),
                    out var width,
                    out var height,
                    out var channels
                );

                var byteCount = (uint)(width * height * channels);
                cmdbuf.SetTextureData(slice, pixels, byteCount);

                Refresh.Refresh_Image_Free(pixels);
            }

            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Update(System.TimeSpan delta)
        {
            if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Left))
            {
                scale = System.MathF.Max(0.01f, scale - 0.01f);
            }

            if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Right))
            {
                scale = System.MathF.Min(1f, scale + 0.01f);
            }
        }

        protected override void Draw(double alpha)
        {
            VertexUniforms vertUniforms = new VertexUniforms(Matrix4x4.CreateScale(scale, scale, 1));

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer != null)
            {
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, Color.Black));
                cmdbuf.BindGraphicsPipeline(pipeline);
                cmdbuf.BindVertexBuffers(vertexBuffer);
                cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
                cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
                uint vertParamOffset = cmdbuf.PushVertexShaderUniforms(vertUniforms);
                cmdbuf.DrawIndexedPrimitives(0, 0, 2, vertParamOffset, 0);
                cmdbuf.EndRenderPass();
            }
            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            TextureMipmapsGame game = new TextureMipmapsGame();
            game.Run();
        }
    }
}
