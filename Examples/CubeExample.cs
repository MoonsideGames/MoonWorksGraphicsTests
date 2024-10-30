using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using MoonWorks.Math;
using MoonWorks.Math.Float;
using System;
using System.IO;
using System.Threading.Tasks;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksGraphicsTests
{
	readonly record struct DepthUniforms(float ZNear, float ZFar);

	class CubeExample : Example
	{
		private GraphicsPipeline CubePipeline;
		private GraphicsPipeline CubePipelineDepthOnly;
		private GraphicsPipeline SkyboxPipeline;
		private GraphicsPipeline SkyboxPipelineDepthOnly;
		private GraphicsPipeline BlitPipeline;
		private GraphicsPipeline FontPipeline;

		private Texture DepthTexture;
		private Sampler DepthSampler;
		private DepthUniforms DepthUniforms;

		private Buffer CubeVertexBuffer;
		private Buffer skyboxVertexBuffer;
		private Buffer BlitVertexBuffer;
		private Buffer IndexBuffer;

		private Texture RenderTexture;
		private TransferBuffer ScreenshotTransferBuffer;
		private Fence ScreenshotFence;

		private Texture SkyboxTexture;
		private Sampler SkyboxSampler;

		private Font SofiaSans;
		private TextBatch TextBatch;

		private bool takeScreenshot;
		private bool screenshotInProgress;
		private bool swapchainDownloaded; // don't want to take screenshot if the swapchain was invalid

		private bool finishedLoading;
		private float cubeTimer;
		private Quaternion cubeRotation;
		private Quaternion previousCubeRotation;
		private bool depthOnlyEnabled;
		private Vector3 camPos;

		private Task LoadingTask;

		// Upload cubemap layers one at a time to minimize transfer size
		unsafe void LoadCubemap(string[] imagePaths)
		{
			var cubemapUploader = new ResourceUploader(
				GraphicsDevice,
				Texture.CalculateSize(
					TextureFormat.R8G8B8A8Unorm,
					SkyboxTexture.Width,
					SkyboxTexture.Height,
					1
				)
			);

			for (uint i = 0; i < imagePaths.Length; i++)
			{
				var textureRegion = new TextureRegion
				{
					Texture = SkyboxTexture.Handle,
					Layer = i,
					W = SkyboxTexture.Width,
					H = SkyboxTexture.Height,
					D = 1
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

			Shader cubeVertShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("PositionColorWithMatrix.vert"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Vertex,
					NumUniformBuffers = 1
				}
			);

			Shader cubeFragShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("SolidColor.frag"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Fragment
				}
			);

			Shader skyboxVertShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("Skybox.vert"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Vertex,
					NumUniformBuffers = 1
				}
			);

			Shader skyboxFragShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("Skybox.frag"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Fragment,
					NumSamplers = 1
				}
			);

			Shader blitVertShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("TexturedQuad.vert"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Vertex
				}
			);

			Shader blitFragShader = ShaderCross.Create(
				GraphicsDevice,
				TestUtils.GetHLSLPath("TexturedDepthQuad.frag"),
				"main",
				new ShaderCross.ShaderCreateInfo
				{
					Format = ShaderCross.ShaderFormat.HLSL,
					Stage = ShaderStage.Fragment,
					NumSamplers = 1,
					NumUniformBuffers = 1
				}
			);

			DepthTexture = Texture.Create2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				TextureFormat.D16Unorm,
				TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
			);
			DepthTexture.Name = "Depth Texture";

			DepthSampler = Sampler.Create(GraphicsDevice, new SamplerCreateInfo());
			DepthUniforms = new DepthUniforms(0.01f, 100f);

			SkyboxTexture = Texture.CreateCube(
				GraphicsDevice,
				2048,
				TextureFormat.R8G8B8A8Unorm,
				TextureUsageFlags.Sampler
			);
			SkyboxTexture.Name = "Skybox";

			SkyboxSampler = Sampler.Create(GraphicsDevice, new SamplerCreateInfo());

			ScreenshotTransferBuffer = TransferBuffer.Create<Color>(
				GraphicsDevice,
				TransferBufferUsage.Download,
				Window.Width * Window.Height
			);
			RenderTexture = Texture.Create2D(
				GraphicsDevice,
				Window.Width,
				Window.Height,
				Window.SwapchainFormat,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
			);
			RenderTexture.Name = "Render Texture";

			LoadingTask = Task.Run(() => UploadGPUAssets());

			// Create the cube pipelines

			GraphicsPipelineCreateInfo cubePipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					ColorTargetDescriptions = [
						new ColorTargetDescription
						{
							Format = Window.SwapchainFormat,
							BlendState = ColorTargetBlendState.Opaque
						}
					],
					HasDepthStencilTarget = true,
					DepthStencilFormat = TextureFormat.D16Unorm
				},
				DepthStencilState = new DepthStencilState
				{
					EnableDepthTest = true,
					EnableDepthWrite = true,
					CompareOp = CompareOp.LessOrEqual
				},
				VertexInputState = VertexInputState.CreateSingleBinding<PositionColorVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullBack,
				MultisampleState = MultisampleState.None,
				VertexShader = cubeVertShader,
				FragmentShader = cubeFragShader
			};
			CubePipeline = GraphicsPipeline.Create(GraphicsDevice, cubePipelineCreateInfo);

			cubePipelineCreateInfo.TargetInfo = new GraphicsPipelineTargetInfo
			{
				HasDepthStencilTarget = true,
				DepthStencilFormat = TextureFormat.D16Unorm
			};
			CubePipelineDepthOnly = GraphicsPipeline.Create(GraphicsDevice, cubePipelineCreateInfo);

			// Create the skybox pipelines

			GraphicsPipelineCreateInfo skyboxPipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					ColorTargetDescriptions = [
						new ColorTargetDescription
						{
							Format = window.SwapchainFormat,
							BlendState = ColorTargetBlendState.Opaque
						}
					],
					HasDepthStencilTarget = true,
					DepthStencilFormat = TextureFormat.D16Unorm
				},
				DepthStencilState = new DepthStencilState
				{
					EnableDepthTest = true,
					EnableDepthWrite = true,
					CompareOp = CompareOp.LessOrEqual
				},
				VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullNone,
				MultisampleState = MultisampleState.None,
				VertexShader = skyboxVertShader,
				FragmentShader = skyboxFragShader
			};
			SkyboxPipeline = GraphicsPipeline.Create(GraphicsDevice, skyboxPipelineCreateInfo);

			skyboxPipelineCreateInfo.TargetInfo = new GraphicsPipelineTargetInfo
			{
				HasDepthStencilTarget = true,
				DepthStencilFormat = TextureFormat.D16Unorm
			};
			SkyboxPipelineDepthOnly = GraphicsPipeline.Create(GraphicsDevice, skyboxPipelineCreateInfo);

			// Create the blit pipeline

			GraphicsPipelineCreateInfo blitPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				Window.SwapchainFormat,
				blitVertShader,
				blitFragShader
			);
			blitPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

			BlitPipeline = GraphicsPipeline.Create(GraphicsDevice, blitPipelineCreateInfo);

			SofiaSans = Font.Load(GraphicsDevice, TestUtils.GetFontPath("SofiaSans.ttf"));
			TextBatch = new TextBatch(GraphicsDevice);

			var fontPipelineCreateInfo = new GraphicsPipelineCreateInfo
			{
				VertexShader = GraphicsDevice.TextVertexShader,
				FragmentShader = GraphicsDevice.TextFragmentShader,
				VertexInputState = GraphicsDevice.TextVertexInputState,
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CCW_CullNone,
				MultisampleState = MultisampleState.None,
				DepthStencilState = DepthStencilState.Disable,
				TargetInfo = new GraphicsPipelineTargetInfo
				{
					ColorTargetDescriptions = [
						new ColorTargetDescription
						{
							Format = Window.SwapchainFormat,
							BlendState = ColorTargetBlendState.PremultipliedAlphaBlend
						}
					]
				}
			};

			FontPipeline = GraphicsPipeline.Create(GraphicsDevice, fontPipelineCreateInfo);
		}

		private void UploadGPUAssets()
		{
			Logger.LogInfo("Loading...");

            Span<PositionColorVertex> cubeVertexData = [
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
			];

            Span<PositionVertex> skyboxVertexData = [
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
			];

            Span<uint> indexData = [
				0, 1, 2,    0, 2, 3,
				6, 5, 4,    7, 6, 4,
				8, 9, 10,   8, 10, 11,
				14, 13, 12, 15, 14, 12,
				16, 17, 18, 16, 18, 19,
				22, 21, 20, 23, 22, 20
			];

            Span<PositionTextureVertex> blitVertexData = [
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 0)),
			];

			var resourceUploader = new ResourceUploader(GraphicsDevice, 1024 * 1024);

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

			LoadCubemap(
            [
                TestUtils.GetTexturePath("right.png"),
				TestUtils.GetTexturePath("left.png"),
				TestUtils.GetTexturePath("top.png"),
				TestUtils.GetTexturePath("bottom.png"),
				TestUtils.GetTexturePath("front.png"),
				TestUtils.GetTexturePath("back.png")
			]);

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
					Matrix4x4 fontProj = Matrix4x4.CreateOrthographicOffCenter(
						0,
						640,
						480,
						0,
						0,
						-1
					);
					Matrix4x4 fontModel =
						Matrix4x4.CreateTranslation(635, 480, 0);

					TextBatch.Start(SofiaSans);
					TextBatch.Add(
						"LOADING...",
						48,
						Color.Black,
						HorizontalAlignment.Right,
						VerticalAlignment.Bottom
					);
					TextBatch.UploadBufferData(cmdbuf);

					float sine = System.MathF.Abs(System.MathF.Sin(cubeTimer));
					Color clearColor = new Color(sine, sine, sine);

					// Clear screen and draw Loading text
					var renderPass = cmdbuf.BeginRenderPass(
						new ColorTargetInfo(swapchainTexture, clearColor)
					);
					renderPass.BindGraphicsPipeline(FontPipeline);
					TextBatch.Render(cmdbuf, renderPass, fontModel * fontProj);
					cmdbuf.EndRenderPass(renderPass);
				}
				else
				{
					RenderPass renderPass;

                    if (depthOnlyEnabled)
                    {
                        renderPass = cmdbuf.BeginRenderPass(
                            new DepthStencilTargetInfo(DepthTexture, 1f, true)
							{
								StoreOp = StoreOp.Store
							}
                        );
                    }
                    else
                    {
                        renderPass = cmdbuf.BeginRenderPass(
                            new ColorTargetInfo(RenderTexture, LoadOp.DontCare, true),
                            new DepthStencilTargetInfo(DepthTexture, 1f, true)
                        );
                    }

                    // Draw cube
                    renderPass.BindGraphicsPipeline(depthOnlyEnabled ? CubePipelineDepthOnly : CubePipeline);
					renderPass.BindVertexBuffer(CubeVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					cmdbuf.PushVertexUniformData(cubeUniforms);
					renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);

					// Draw skybox
					renderPass.BindGraphicsPipeline(depthOnlyEnabled ? SkyboxPipelineDepthOnly : SkyboxPipeline);
					renderPass.BindVertexBuffer(skyboxVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					renderPass.BindFragmentSampler(new TextureSamplerBinding(SkyboxTexture, SkyboxSampler));
					cmdbuf.PushVertexUniformData(skyboxUniforms);
					renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);

					cmdbuf.EndRenderPass(renderPass);

					if (depthOnlyEnabled)
					{
						// Draw the depth buffer as a grayscale image
						renderPass = cmdbuf.BeginRenderPass(
							new ColorTargetInfo(RenderTexture, LoadOp.Load)
						);

						renderPass.BindGraphicsPipeline(BlitPipeline);
						renderPass.BindFragmentSampler(new TextureSamplerBinding(DepthTexture, DepthSampler));
						renderPass.BindVertexBuffer(BlitVertexBuffer);
						cmdbuf.PushFragmentUniformData(DepthUniforms);
						renderPass.DrawPrimitives(6, 1, 0, 0);

						cmdbuf.EndRenderPass(renderPass);
					}

					cmdbuf.Blit(RenderTexture, swapchainTexture, Filter.Nearest);

					if (takeScreenshot)
					{
						var copyPass = cmdbuf.BeginCopyPass();
						copyPass.DownloadFromTexture(
							new TextureRegion(RenderTexture),
							new TextureTransferInfo(ScreenshotTransferBuffer)
						);
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

			var screenshotSpan = ScreenshotTransferBuffer.Map<Color>(false);
			ImageUtils.SavePNG(
				Path.Combine(System.AppContext.BaseDirectory, "screenshot.png"),
				screenshotSpan,
				RenderTexture.Width,
				RenderTexture.Height,
				RenderTexture.Format == TextureFormat.B8G8R8A8Unorm
			);
			ScreenshotTransferBuffer.Unmap();

			GraphicsDevice.ReleaseFence(ScreenshotFence);
			ScreenshotFence = null;

			screenshotInProgress = false;
		}

        public override void Destroy()
        {
			LoadingTask.Wait();

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
			RenderTexture.Dispose();

			SkyboxTexture.Dispose();
			SkyboxSampler.Dispose();

			TextBatch.Dispose();
			SofiaSans.Dispose();
        }
	}
}
