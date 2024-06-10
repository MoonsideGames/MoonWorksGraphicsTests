using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MoonWorksGraphicsTests
{
	struct DepthUniforms
	{
		public float ZNear;
		public float ZFar;

		public DepthUniforms(float zNear, float zFar)
		{
			ZNear = zNear;
			ZFar = zFar;
		}
	}

	class CubeExample : Example
	{
		private GraphicsPipeline CubePipeline;
		private GraphicsPipeline CubePipelineDepthOnly;
		private GraphicsPipeline SkyboxPipeline;
		private GraphicsPipeline SkyboxPipelineDepthOnly;
		private GraphicsPipeline BlitPipeline;

		private Texture DepthTexture;
		private Sampler DepthSampler;
		private DepthUniforms DepthUniforms;

		private GpuBuffer CubeVertexBuffer;
		private GpuBuffer skyboxVertexBuffer;
		private GpuBuffer BlitVertexBuffer;
		private GpuBuffer IndexBuffer;

		private TransferBuffer ScreenshotTransferBuffer;
		private Texture ScreenshotTexture;
		private Fence ScreenshotFence;

		private Texture SkyboxTexture;
		private Sampler SkyboxSampler;

		private bool takeScreenshot;
		private bool screenshotInProgress;
		private bool swapchainDownloaded; // don't want to take screenshot if the swapchain was invalid

		private bool finishedLoading;
		private float cubeTimer;
		private Quaternion cubeRotation;
		private Quaternion previousCubeRotation;
		private bool depthOnlyEnabled;
		private Vector3 camPos;

		// Upload cubemap layers one at a time to minimize transfer size
		unsafe void LoadCubemap(string[] imagePaths)
		{
			var cubemapUploader = new ResourceUploader(GraphicsDevice);

			for (uint i = 0; i < imagePaths.Length; i++)
			{
				var textureRegion = new TextureRegion
				{
					TextureSlice = new TextureSlice
					{
						Texture = SkyboxTexture,
						MipLevel = 0,
						Layer = i,
					},
					X = 0,
					Y = 0,
					Z = 0,
					Width = SkyboxTexture.Width,
					Height = SkyboxTexture.Height,
					Depth = 1
				};

				cubemapUploader.SetTextureDataFromCompressed(
					textureRegion,
					imagePaths[i]
				);

				cubemapUploader.UploadAndWait();
			}

			cubemapUploader.Dispose();
		}

        public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
        {
			Window = window;
			GraphicsDevice = graphicsDevice;
			Inputs = inputs;

			Window.SetTitle("Cube");

			finishedLoading = false;
			cubeTimer = 0;
			cubeRotation = Quaternion.Identity;
			previousCubeRotation = Quaternion.Identity;
			depthOnlyEnabled = false;
			camPos = new Vector3(0, 1.5f, 4);

			Shader cubeVertShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("PositionColorWithMatrix.vert"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Vertex,
					ShaderFormat = ShaderFormat.SPIRV,
					UniformBufferCount = 1
				}
			);

			Shader cubeFragShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("SolidColor.frag"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Fragment,
					ShaderFormat = ShaderFormat.SPIRV
				}
			);

			Shader skyboxVertShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("Skybox.vert"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Vertex,
					ShaderFormat = ShaderFormat.SPIRV,
					UniformBufferCount = 1
				}
			);

			Shader skyboxFragShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("Skybox.frag"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Fragment,
					ShaderFormat = ShaderFormat.SPIRV,
					SamplerCount = 1
				}
			);

			Shader blitVertShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("TexturedQuad.vert"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Vertex,
					ShaderFormat = ShaderFormat.SPIRV
				}
			);

			Shader blitFragShader = new Shader(
				GraphicsDevice,
				TestUtils.GetShaderPath("TexturedDepthQuad.frag"),
				"main",
				new ShaderCreateInfo
				{
					ShaderStage = ShaderStage.Fragment,
					ShaderFormat = ShaderFormat.SPIRV,
					SamplerCount = 1,
					UniformBufferCount = 1
				}
			);

			DepthTexture = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				TextureFormat.D16_UNORM,
				TextureUsageFlags.DepthStencil | TextureUsageFlags.Sampler
			);
			DepthTexture.Name = "Depth Texture";

			DepthSampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());
			DepthUniforms = new DepthUniforms(0.01f, 100f);

			SkyboxTexture = Texture.CreateTextureCube(
				GraphicsDevice,
				2048,
				TextureFormat.R8G8B8A8,
				TextureUsageFlags.Sampler
			);
			SkyboxTexture.Name = "Skybox";

			SkyboxSampler = new Sampler(GraphicsDevice, new SamplerCreateInfo());

			ScreenshotTransferBuffer = new TransferBuffer(
				GraphicsDevice,
				TransferUsage.Texture,
				TransferBufferMapFlags.Read,
				Window.Width * Window.Height * 4
			);
			ScreenshotTexture = Texture.CreateTexture2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				Window.SwapchainFormat,
				TextureUsageFlags.Sampler
			);
			ScreenshotTexture.Name = "Screenshot";

			Task loadingTask = Task.Run(() => UploadGPUAssets());

			// Create the cube pipelines

			GraphicsPipelineCreateInfo cubePipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
					TextureFormat.D16_UNORM,
					new ColorAttachmentDescription(
						Window.SwapchainFormat,
						ColorAttachmentBlendState.Opaque
					)
				),
				DepthStencilState = DepthStencilState.DepthReadWrite,
				VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullBack,
				MultisampleState = MultisampleState.None,
				VertexShader = cubeVertShader,
				FragmentShader = cubeFragShader
			};
			CubePipeline = new GraphicsPipeline(GraphicsDevice, cubePipelineCreateInfo);

			cubePipelineCreateInfo.AttachmentInfo = new GraphicsPipelineAttachmentInfo(TextureFormat.D16_UNORM);
			CubePipelineDepthOnly = new GraphicsPipeline(GraphicsDevice, cubePipelineCreateInfo);

			// Create the skybox pipelines

			GraphicsPipelineCreateInfo skyboxPipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				AttachmentInfo = new GraphicsPipelineAttachmentInfo(
						TextureFormat.D16_UNORM,
						new ColorAttachmentDescription(
							Window.SwapchainFormat,
							ColorAttachmentBlendState.Opaque
						)
					),
				DepthStencilState = DepthStencilState.DepthReadWrite,
				VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullNone,
				MultisampleState = MultisampleState.None,
				VertexShader = skyboxVertShader,
				FragmentShader = skyboxFragShader
			};
			SkyboxPipeline = new GraphicsPipeline(GraphicsDevice, skyboxPipelineCreateInfo);

			skyboxPipelineCreateInfo.AttachmentInfo = new GraphicsPipelineAttachmentInfo(TextureFormat.D16_UNORM);
			SkyboxPipelineDepthOnly = new GraphicsPipeline(GraphicsDevice, skyboxPipelineCreateInfo);

			// Create the blit pipeline

			GraphicsPipelineCreateInfo blitPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				Window.SwapchainFormat,
				blitVertShader,
				blitFragShader
			);
			blitPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

			BlitPipeline = new GraphicsPipeline(GraphicsDevice, blitPipelineCreateInfo);
		}

		private void UploadGPUAssets()
		{
			Logger.LogInfo("Loading...");

			var cubeVertexData = new Span<PositionColorVertex>([
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
			]);

			var skyboxVertexData = new Span<PositionVertex>([
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
			]);

			var indexData = new Span<uint>([
				0, 1, 2,    0, 2, 3,
				6, 5, 4,    7, 6, 4,
				8, 9, 10,   8, 10, 11,
				14, 13, 12, 15, 14, 12,
				16, 17, 18, 16, 18, 19,
				22, 21, 20, 23, 22, 20
			]);

			var blitVertexData = new Span<PositionTextureVertex>([
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
			]);

			var resourceUploader = new ResourceUploader(GraphicsDevice);

			CubeVertexBuffer = resourceUploader.CreateBuffer(cubeVertexData, BufferUsageFlags.Vertex);
			skyboxVertexBuffer = resourceUploader.CreateBuffer(skyboxVertexData, BufferUsageFlags.Vertex);
			IndexBuffer = resourceUploader.CreateBuffer(indexData, BufferUsageFlags.Index);
			BlitVertexBuffer = resourceUploader.CreateBuffer(blitVertexData, BufferUsageFlags.Vertex);

			CubeVertexBuffer.Name = "Cube Vertices";
			skyboxVertexBuffer.Name = "Skybox Vertices";
			IndexBuffer.Name = "Cube Indices";
			BlitVertexBuffer.Name = "Blit Vertices";

			resourceUploader.Upload();
			resourceUploader.Dispose();

			LoadCubemap(new string[]
			{
				TestUtils.GetTexturePath("right.png"),
				TestUtils.GetTexturePath("left.png"),
				TestUtils.GetTexturePath("top.png"),
				TestUtils.GetTexturePath("bottom.png"),
				TestUtils.GetTexturePath("front.png"),
				TestUtils.GetTexturePath("back.png")
			});

			finishedLoading = true;
			Logger.LogInfo("Finished loading!");
			Logger.LogInfo("Press Left to toggle Depth-Only Mode");
			Logger.LogInfo("Press Down to move the camera upwards");
			Logger.LogInfo("Press Right to save a screenshot");
		}

		public override void Update(System.TimeSpan delta)
		{
			cubeTimer += (float) delta.TotalSeconds;

			previousCubeRotation = cubeRotation;

			cubeRotation = Quaternion.CreateFromYawPitchRoll(
				cubeTimer * 2f,
				0,
				cubeTimer * 2f
			);

			if (TestUtils.CheckButtonDown(Inputs, TestUtils.ButtonType.Bottom))
			{
				camPos.Y = System.MathF.Min(camPos.Y + 0.2f, 15f);
			}
			else
			{
				camPos.Y = System.MathF.Max(camPos.Y - 0.4f, 1.5f);
			}

			if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Left))
			{
				depthOnlyEnabled = !depthOnlyEnabled;
				Logger.LogInfo("Depth-Only Mode enabled: " + depthOnlyEnabled);
			}

			if (!screenshotInProgress && TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
			{
				takeScreenshot = true;
			}
		}

		public override void Draw(double alpha)
		{
			Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(75f),
				(float) Window.Width / Window.Height,
				DepthUniforms.ZNear,
				DepthUniforms.ZFar
			);
			Matrix4x4 view = Matrix4x4.CreateLookAt(
				camPos,
				Vector3.Zero,
				Vector3.Up
			);
			TransformVertexUniform skyboxUniforms = new TransformVertexUniform(view * proj);

			Matrix4x4 model = Matrix4x4.CreateFromQuaternion(
				Quaternion.Slerp(
					previousCubeRotation,
					cubeRotation,
					(float) alpha
				)
			);
			TransformVertexUniform cubeUniforms = new TransformVertexUniform(model * view * proj);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
			if (swapchainTexture != null)
			{
				if (!finishedLoading)
				{
					float sine = System.MathF.Abs(System.MathF.Sin(cubeTimer));
					Color clearColor = new Color(sine, sine, sine);

					// Just show a clear screen.
					var renderPass = cmdbuf.BeginRenderPass(
						new ColorAttachmentInfo(
							swapchainTexture,
							false,
							clearColor
						)
					);
					cmdbuf.EndRenderPass(renderPass);
				}
				else
				{
					RenderPass renderPass;

					if (!depthOnlyEnabled)
					{
						renderPass = cmdbuf.BeginRenderPass(
							new DepthStencilAttachmentInfo(DepthTexture, true, new DepthStencilValue(1f, 0)),
							new ColorAttachmentInfo(swapchainTexture, false, LoadOp.DontCare)
						);
					}
					else
					{
						renderPass = cmdbuf.BeginRenderPass(
							new DepthStencilAttachmentInfo(DepthTexture, true, new DepthStencilValue(1f, 0), StoreOp.Store)
						);
					}

					// Draw cube
					renderPass.BindGraphicsPipeline(depthOnlyEnabled ? CubePipelineDepthOnly : CubePipeline);
					renderPass.BindVertexBuffer(CubeVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					renderPass.PushVertexUniformData(cubeUniforms);
					renderPass.DrawIndexedPrimitives(0, 0, 12);

					// Draw skybox
					renderPass.BindGraphicsPipeline(depthOnlyEnabled ? SkyboxPipelineDepthOnly : SkyboxPipeline);
					renderPass.BindVertexBuffer(skyboxVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					renderPass.BindFragmentSampler(new TextureSamplerBinding(SkyboxTexture, SkyboxSampler));
					renderPass.PushVertexUniformData(skyboxUniforms);
					renderPass.DrawIndexedPrimitives(0, 0, 12);

					cmdbuf.EndRenderPass(renderPass);

					if (depthOnlyEnabled)
					{
						// Draw the depth buffer as a grayscale image
						renderPass = cmdbuf.BeginRenderPass(
							new ColorAttachmentInfo(
								swapchainTexture,
								false,
								LoadOp.Load
							)
						);

						renderPass.BindGraphicsPipeline(BlitPipeline);
						renderPass.BindFragmentSampler(new TextureSamplerBinding(DepthTexture, DepthSampler));
						renderPass.BindVertexBuffer(BlitVertexBuffer);
						renderPass.PushFragmentUniformData(DepthUniforms);
						renderPass.DrawPrimitives(0, 2);

						cmdbuf.EndRenderPass(renderPass);
					}

					if (takeScreenshot)
					{
						var copyPass = cmdbuf.BeginCopyPass();
						copyPass.DownloadFromTexture(swapchainTexture, ScreenshotTransferBuffer, new BufferImageCopy(0, 0, 0));
						cmdbuf.EndCopyPass(copyPass);

						swapchainDownloaded = true;
					}
				}
			}

			if (takeScreenshot && swapchainDownloaded)
			{
				ScreenshotFence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
				Task.Run(TakeScreenshot);

				takeScreenshot = false;
				swapchainDownloaded = false;
			}
			else
			{
				GraphicsDevice.Submit(cmdbuf);
			}
		}

		private unsafe void TakeScreenshot()
		{
			screenshotInProgress = true;

			GraphicsDevice.WaitForFence(ScreenshotFence);

			ImageUtils.SavePNG(
				Path.Combine(System.AppContext.BaseDirectory, "screenshot.png"),
				ScreenshotTransferBuffer,
				0,
				(int) ScreenshotTexture.Width,
				(int) ScreenshotTexture.Height,
				ScreenshotTexture.Format == TextureFormat.B8G8R8A8
			);

			GraphicsDevice.ReleaseFence(ScreenshotFence);
			ScreenshotFence = null;

			screenshotInProgress = false;
		}

        public override void Destroy()
        {
            CubePipeline.Dispose();
			CubePipelineDepthOnly.Dispose();
			SkyboxPipeline.Dispose();
			SkyboxPipelineDepthOnly.Dispose();
			BlitPipeline.Dispose();

			DepthTexture.Dispose();
			DepthSampler.Dispose();

			CubeVertexBuffer.Dispose();
			skyboxVertexBuffer.Dispose();
			BlitVertexBuffer.Dispose();
			IndexBuffer.Dispose();

			ScreenshotTransferBuffer.Dispose();
			ScreenshotTexture.Dispose();

			SkyboxTexture.Dispose();
			SkyboxSampler.Dispose();
        }
	}
}
