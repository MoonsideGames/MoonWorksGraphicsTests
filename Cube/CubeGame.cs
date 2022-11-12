using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using System.Threading.Tasks;

namespace MoonWorks.Test
{
    class CubeGame : Game
    {
        private GraphicsPipeline cubePipeline;
        private GraphicsPipeline skyboxPipeline;
        private Texture depthTexture;
        private Buffer cubeVertexBuffer;
        private Buffer skyboxVertexBuffer;
        private Buffer indexBuffer;
        private Texture skyboxTexture;
        private Sampler skyboxSampler;
        private bool finishedLoading;

        private float cubeTimer = 0f;
        private Quaternion cubeRotation = Quaternion.Identity;
        private Quaternion previousCubeRotation = Quaternion.Identity;

        struct Uniforms
        {
            public Matrix4x4 ViewProjection;

            public Uniforms(Matrix4x4 viewProjection)
            {
                ViewProjection = viewProjection;
            }
        }

        void LoadCubemap(CommandBuffer cmdbuf, string[] imagePaths)
        {
            System.IntPtr textureData;
            int w, h, numChannels;

            for (uint i = 0; i < imagePaths.Length; i++)
            {
                textureData = RefreshCS.Refresh.Refresh_Image_Load(
                    imagePaths[i],
                    out w,
                    out h,
                    out numChannels
                );
                cmdbuf.SetTextureData(
                    new TextureSlice(
                        skyboxTexture,
                        new Rect(0, 0, w, h),
                        0,
                        i
                    ),
                    textureData,
                    (uint) (w * h * 4) // w * h * numChannels does not work
                );
                RefreshCS.Refresh.Refresh_Image_Free(textureData);
            }
        }

        public CubeGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            ShaderModule cubeVertShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("PositionColorVertWithMatrix.spv")
            );
            ShaderModule cubeFragShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("SolidColor.spv")
            );

            ShaderModule skyboxVertShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("SkyboxVert.spv")
            );
            ShaderModule skyboxFragShaderModule = new ShaderModule(
                GraphicsDevice,
                TestUtils.GetShaderPath("SkyboxFrag.spv")
            );

            depthTexture = Texture.CreateTexture2D(
                GraphicsDevice,
                MainWindow.Width,
                MainWindow.Height,
                TextureFormat.D16,
                TextureUsageFlags.DepthStencilTarget
            );

            skyboxTexture = Texture.CreateTextureCube(
                GraphicsDevice,
                2048,
                TextureFormat.R8G8B8A8,
                TextureUsageFlags.Sampler
            );
            skyboxSampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

            cubeVertexBuffer = Buffer.Create<PositionColorVertex>(
                GraphicsDevice,
                BufferUsageFlags.Vertex,
                24
            );
            skyboxVertexBuffer = Buffer.Create<PositionVertex>(
                GraphicsDevice,
                BufferUsageFlags.Vertex,
                24
            );
            indexBuffer = Buffer.Create<uint>(
                GraphicsDevice,
                BufferUsageFlags.Index,
                36
            ); // Using uint here just to test IndexElementSize=32

            Task loadingTask = Task.Run(() => UploadGPUAssets());

            cubePipeline = new GraphicsPipeline(
                GraphicsDevice,
                new GraphicsPipelineCreateInfo
                {
                    AttachmentInfo = new GraphicsPipelineAttachmentInfo(
                        TextureFormat.D16,
                        new ColorAttachmentDescription(
                            MainWindow.SwapchainFormat,
                            ColorAttachmentBlendState.Opaque
                        )
                    ),
                    DepthStencilState = DepthStencilState.DepthReadWrite,
                    VertexShaderInfo = GraphicsShaderInfo.Create<Uniforms>(cubeVertShaderModule, "main", 0),
                    VertexInputState = new VertexInputState(
                        VertexBinding.Create<PositionColorVertex>(),
                        VertexAttribute.Create<PositionColorVertex>("Position", 0),
                        VertexAttribute.Create<PositionColorVertex>("Color", 1)
                    ),
                    PrimitiveType = PrimitiveType.TriangleList,
                    FragmentShaderInfo = GraphicsShaderInfo.Create(cubeFragShaderModule, "main", 0),
                    RasterizerState = RasterizerState.CW_CullBack,
                    MultisampleState = MultisampleState.None
                }
            );

            skyboxPipeline = new GraphicsPipeline(
                GraphicsDevice,
                new GraphicsPipelineCreateInfo
                {
                    AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					    TextureFormat.D16,
					    new ColorAttachmentDescription(
						    MainWindow.SwapchainFormat,
						    ColorAttachmentBlendState.Opaque
					    )
				    ),
				    DepthStencilState = DepthStencilState.DepthReadWrite,
				    VertexShaderInfo = GraphicsShaderInfo.Create<Uniforms>(skyboxVertShaderModule, "main", 0),
				    VertexInputState = new VertexInputState(
					    VertexBinding.Create<PositionVertex>(),
					    VertexAttribute.Create<PositionVertex>("Position", 0)
				    ),
				    PrimitiveType = PrimitiveType.TriangleList,
				    FragmentShaderInfo = GraphicsShaderInfo.Create(skyboxFragShaderModule, "main", 1),
				    RasterizerState = RasterizerState.CW_CullNone,
				    MultisampleState = MultisampleState.None,
                }
            );
        }

        private void UploadGPUAssets()
        {
            Logger.LogInfo("Loading...");

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

            cmdbuf.SetBufferData(
                cubeVertexBuffer,
                new PositionColorVertex[]
                {
                    new PositionColorVertex(new Vector3(-1, -1, -1), new Color(1f, 0f, 0f)),
                    new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, -1), new Color(1f, 0f, 0f)),
                    new PositionColorVertex(new Vector3(-1, 1, -1), new Color(1f, 0f, 0f)),

                    new PositionColorVertex(new Vector3(-1, -1, 1), new Color(0f, 1f, 0f)),
                    new PositionColorVertex(new Vector3(1, -1, 1), new Color(0f, 1f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, 1), new Color(0f, 1f, 0f)),
                    new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 1f, 0f)),

                    new PositionColorVertex(new Vector3(-1, -1, -1), new Color(0f, 0f, 1f)),
                    new PositionColorVertex(new Vector3(-1, 1, -1), new Color(0f, 0f, 1f)),
                    new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 0f, 1f)),
                    new PositionColorVertex(new Vector3(-1, -1, 1), new Color(0f, 0f, 1f)),

                    new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, -1), new Color(1f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, 1), new Color(1f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(1, -1, 1), new Color(1f, 0.5f, 0f)),

                    new PositionColorVertex(new Vector3(-1, -1, -1), new Color(1f, 0f, 0.5f)),
                    new PositionColorVertex(new Vector3(-1, -1, 1), new Color(1f, 0f, 0.5f)),
                    new PositionColorVertex(new Vector3(1, -1, 1), new Color(1f, 0f, 0.5f)),
                    new PositionColorVertex(new Vector3(1, -1, -1), new Color(1f, 0f, 0.5f)),

                    new PositionColorVertex(new Vector3(-1, 1, -1), new Color(0f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(-1, 1, 1), new Color(0f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, 1), new Color(0f, 0.5f, 0f)),
                    new PositionColorVertex(new Vector3(1, 1, -1), new Color(0f, 0.5f, 0f))
                }
            );

            cmdbuf.SetBufferData(
			    skyboxVertexBuffer,
			    new PositionVertex[]
			    {
				    new PositionVertex(new Vector3(-10, -10, -10)),
				    new PositionVertex(new Vector3(10, -10, -10)),
				    new PositionVertex(new Vector3(10, 10, -10)),
				    new PositionVertex(new Vector3(-10, 10, -10)),

				    new PositionVertex(new Vector3(-10, -10, 10)),
				    new PositionVertex(new Vector3(10, -10, 10)),
				    new PositionVertex(new Vector3(10, 10, 10)),
				    new PositionVertex(new Vector3(-10, 10, 10)),

				    new PositionVertex(new Vector3(-10, -10, -10)),
				    new PositionVertex(new Vector3(-10, 10, -10)),
				    new PositionVertex(new Vector3(-10, 10, 10)),
				    new PositionVertex(new Vector3(-10, -10, 10)),

				    new PositionVertex(new Vector3(10, -10, -10)),
				    new PositionVertex(new Vector3(10, 10, -10)),
				    new PositionVertex(new Vector3(10, 10, 10)),
				    new PositionVertex(new Vector3(10, -10, 10)),

				    new PositionVertex(new Vector3(-10, -10, -10)),
				    new PositionVertex(new Vector3(-10, -10, 10)),
				    new PositionVertex(new Vector3(10, -10, 10)),
				    new PositionVertex(new Vector3(10, -10, -10)),

				    new PositionVertex(new Vector3(-10, 10, -10)),
				    new PositionVertex(new Vector3(-10, 10, 10)),
				    new PositionVertex(new Vector3(10, 10, 10)),
				    new PositionVertex(new Vector3(10, 10, -10))
			    }
		    );

            cmdbuf.SetBufferData(
                indexBuffer,
                new uint[]
                {
                    0, 1, 2,    0, 2, 3,
                    6, 5, 4,    7, 6, 4,
                    8, 9, 10,   8, 10, 11,
                    14, 13, 12, 15, 14, 12,
                    16, 17, 18, 16, 18, 19,
                    22, 21, 20, 23, 22, 20
                }
            );

            LoadCubemap(cmdbuf, new string[]
		    {
			    TestUtils.GetTexturePath("right.png"),
			    TestUtils.GetTexturePath("left.png"),
			    TestUtils.GetTexturePath("top.png"),
			    TestUtils.GetTexturePath("bottom.png"),
			    TestUtils.GetTexturePath("front.png"),
			    TestUtils.GetTexturePath("back.png")
		    });

            GraphicsDevice.Submit(cmdbuf);

            finishedLoading = true;
            Logger.LogInfo("Finished loading!");
        }

        protected override void Update(System.TimeSpan delta)
        {
            cubeTimer += (float) delta.TotalSeconds;

            previousCubeRotation = cubeRotation;

            cubeRotation = Quaternion.CreateFromYawPitchRoll(
                cubeTimer * 2f,
                0,
                cubeTimer * 2f
            );
        }

        protected override void Draw(double alpha)
        {
            Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(75f),
                (float) MainWindow.Width / MainWindow.Height,
                0.01f,
                100f
            );
            Matrix4x4 view = Matrix4x4.CreateLookAt(
                new Vector3(0, 1.5f, 4f),
                Vector3.Zero,
                Vector3.Up
            );
            Uniforms skyboxUniforms = new Uniforms(view * proj);

            Matrix4x4 model = Matrix4x4.CreateFromQuaternion(
                Quaternion.Slerp(
                    previousCubeRotation,
                    cubeRotation,
                    (float) alpha
                )
            );
            Uniforms cubeUniforms = new Uniforms(model * view * proj);

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            Texture? swapchainTexture = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (swapchainTexture != null)
            {
                if (!finishedLoading)
                {
                    float sine = System.MathF.Abs(System.MathF.Sin(cubeTimer));
                    Color clearColor = new Color(sine, sine, sine);

                    // Just show a clear screen.
                    cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchainTexture, clearColor));
                    cmdbuf.EndRenderPass();
                }
                else
                {
                    cmdbuf.BeginRenderPass(
                        new DepthStencilAttachmentInfo(depthTexture, new DepthStencilValue(1f, 0)),
                        new ColorAttachmentInfo(swapchainTexture, Color.CornflowerBlue)
                    );

                    // Draw cube
                    cmdbuf.BindGraphicsPipeline(cubePipeline);
                    cmdbuf.BindVertexBuffers(cubeVertexBuffer);
                    cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.ThirtyTwo);
                    uint vertexParamOffset = cmdbuf.PushVertexShaderUniforms(cubeUniforms);
                    cmdbuf.DrawIndexedPrimitives(0, 0, 12, vertexParamOffset, 0);

                    // Draw skybox
                    cmdbuf.BindGraphicsPipeline(skyboxPipeline);
                    cmdbuf.BindVertexBuffers(skyboxVertexBuffer);
                    cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.ThirtyTwo);
                    cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(skyboxTexture, skyboxSampler));
                    vertexParamOffset = cmdbuf.PushVertexShaderUniforms(skyboxUniforms);
                    cmdbuf.DrawIndexedPrimitives(0, 0, 12, vertexParamOffset, 0);

                    cmdbuf.EndRenderPass();
                }
            }

            GraphicsDevice.Submit(cmdbuf);
        }

        public static void Main(string[] args)
        {
            CubeGame game = new CubeGame();
            game.Run();
        }
    }
}
