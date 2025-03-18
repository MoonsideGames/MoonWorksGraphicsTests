using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using MoonWorks.Storage;
using System;
using System.IO;
using System.Numerics;
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
		private ResultToken screenshotSaveToken;
		private IntPtr screenshotPNGBuffer;
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
					RootTitleStorage,
					imagePaths[i],
					textureRegion
				);

				cubemapUploader.UploadAndWait();
			}

			cubemapUploader.Dispose();
		}

        public override void Init()
        {
			Window.SetTitle("Cube");

			finishedLoading = false;
			cubeTimer = 0;
			cubeRotation = Quaternion.Identity;
			previousCubeRotation = Quaternion.Identity;
			depthOnlyEnabled = false;
			camPos = new Vector3(0, 1.5f, 4);

			Shader cubeVertShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("PositionColorWithMatrix.vert"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Vertex
			);

			Shader cubeFragShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("SolidColor.frag"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Fragment
			);

			Shader skyboxVertShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("Skybox.vert"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Vertex
			);

			Shader skyboxFragShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("Skybox.frag"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Fragment
			);

			Shader blitVertShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("TexturedQuad.vert"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Vertex
			);

			Shader blitFragShader = ShaderCross.Create(
				GraphicsDevice,
				RootTitleStorage,
				TestUtils.GetHLSLPath("TexturedDepthQuad.frag"),
				"main",
				ShaderCross.ShaderFormat.HLSL,
				ShaderStage.Fragment
			);

			DepthTexture = Texture.Create2D(
				GraphicsDevice,
				"Depth Texture",
				Window.Width,
				Window.Height,
				TextureFormat.D16Unorm,
				TextureUsageFlags.DepthStencilTarget | TextureUsageFlags.Sampler
			);

			DepthSampler = Sampler.Create(GraphicsDevice, "Depth Sampler", new SamplerCreateInfo());
			DepthUniforms = new DepthUniforms(0.01f, 100f);

			SkyboxTexture = Texture.CreateCube(
				GraphicsDevice,
				"Skybox",
				2048,
				TextureFormat.R8G8B8A8Unorm,
				TextureUsageFlags.Sampler
			);

			SkyboxSampler = Sampler.Create(GraphicsDevice, "Skybox Sampler", new SamplerCreateInfo());

			ScreenshotTransferBuffer = TransferBuffer.Create<Color>(
				GraphicsDevice,
				TransferBufferUsage.Download,
				Window.Width * Window.Height
			);
			RenderTexture = Texture.Create2D(
				GraphicsDevice,
				"Render Texture",
				Window.Width,
				Window.Height,
				Window.SwapchainFormat,
				TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler
			);

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
				FragmentShader = cubeFragShader,
				Name = "Cube Pipeline"
			};
			CubePipeline = GraphicsPipeline.Create(GraphicsDevice, cubePipelineCreateInfo);

			cubePipelineCreateInfo.TargetInfo = new GraphicsPipelineTargetInfo
			{
				HasDepthStencilTarget = true,
				DepthStencilFormat = TextureFormat.D16Unorm
			};
			cubePipelineCreateInfo.Name = "Depth Only Cube Pipeline";
			CubePipelineDepthOnly = GraphicsPipeline.Create(GraphicsDevice, cubePipelineCreateInfo);

			// Create the skybox pipelines

			GraphicsPipelineCreateInfo skyboxPipelineCreateInfo = new GraphicsPipelineCreateInfo
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
				VertexInputState = VertexInputState.CreateSingleBinding<PositionVertex>(),
				PrimitiveType = PrimitiveType.TriangleList,
				RasterizerState = RasterizerState.CW_CullNone,
				MultisampleState = MultisampleState.None,
				VertexShader = skyboxVertShader,
				FragmentShader = skyboxFragShader,
				Name = "Skybox Pipeline"
			};
			SkyboxPipeline = GraphicsPipeline.Create(GraphicsDevice, skyboxPipelineCreateInfo);

			skyboxPipelineCreateInfo.TargetInfo = new GraphicsPipelineTargetInfo
			{
				HasDepthStencilTarget = true,
				DepthStencilFormat = TextureFormat.D16Unorm
			};
			skyboxPipelineCreateInfo.Name = "Skybox Pipeline Depth Only";
			SkyboxPipelineDepthOnly = GraphicsPipeline.Create(GraphicsDevice, skyboxPipelineCreateInfo);

			// Create the blit pipeline

			GraphicsPipelineCreateInfo blitPipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				Window.SwapchainFormat,
				blitVertShader,
				blitFragShader
			);
			blitPipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();

			BlitPipeline = GraphicsPipeline.Create(GraphicsDevice, blitPipelineCreateInfo);

			SofiaSans = Font.Load(GraphicsDevice, RootTitleStorage, TestUtils.GetFontPath("SofiaSans.ttf"));
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

            ReadOnlySpan<PositionColorVertex> cubeVertexData = [
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

            ReadOnlySpan<PositionVertex> skyboxVertexData = [
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

            ReadOnlySpan<uint> indexData = [
				0, 1, 2,    0, 2, 3,
				6, 5, 4,    7, 6, 4,
				8, 9, 10,   8, 10, 11,
				14, 13, 12, 15, 14, 12,
				16, 17, 18, 16, 18, 19,
				22, 21, 20, 23, 22, 20
			];

            ReadOnlySpan<PositionTextureVertex> blitVertexData = [
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 1)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 0)),
				new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 0)),
			];

			var resourceUploader = new ResourceUploader(GraphicsDevice, 1024 * 1024);

			CubeVertexBuffer = resourceUploader.CreateBuffer("Cube Vertex Buffer", cubeVertexData, BufferUsageFlags.Vertex);
			skyboxVertexBuffer = resourceUploader.CreateBuffer("Skybox Vertex Buffer", skyboxVertexData, BufferUsageFlags.Vertex);
			IndexBuffer = resourceUploader.CreateBuffer("Cube Index Buffer", indexData, BufferUsageFlags.Index);
			BlitVertexBuffer = resourceUploader.CreateBuffer("Blit Vertex Buffer", blitVertexData, BufferUsageFlags.Vertex);

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

			if (screenshotSaveToken != null)
			{
				if (screenshotSaveToken.Result != Result.Pending)
				{
					ImageUtils.FreeBufferData(screenshotPNGBuffer);
					screenshotPNGBuffer = IntPtr.Zero;

					if (screenshotSaveToken.Result == Result.Success)
					{
						Logger.LogInfo("Screenshot saved to user storage!");
					}
					else
					{
						Logger.LogInfo("Screenshot failed to save!");
					}

					UserStorage.ReleaseToken(screenshotSaveToken);
					screenshotSaveToken = null;
				}
			}
			else
			{
				if (TestUtils.CheckButtonPressed(Inputs, TestUtils.ButtonType.Right))
				{
					takeScreenshot = true;
				}
			}
		}

		public override void Draw(double alpha)
		{
			Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(
				float.DegreesToRadians(75f),
				(float) Window.Width / Window.Height,
				DepthUniforms.ZNear,
				DepthUniforms.ZFar
			);
			Matrix4x4 view = Matrix4x4.CreateLookAt(
				camPos,
				Vector3.Zero,
				Vector3.UnitY
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

			var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
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

					TextBatch.Start();
					TextBatch.Add(
						SofiaSans,
						"LOADING...",
						48,
						fontModel,
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
					TextBatch.Render(renderPass, fontProj);
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
							new DepthStencilTargetInfo(DepthTexture, 1f, true),
                            new ColorTargetInfo(RenderTexture, LoadOp.DontCare, true)
                        );
                    }

                    // Draw cube
                    renderPass.BindGraphicsPipeline(depthOnlyEnabled ? CubePipelineDepthOnly : CubePipeline);
					renderPass.BindVertexBuffers(CubeVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					cmdbuf.PushVertexUniformData(cubeUniforms);
					renderPass.DrawIndexedPrimitives(36, 1, 0, 0, 0);

					// Draw skybox
					renderPass.BindGraphicsPipeline(depthOnlyEnabled ? SkyboxPipelineDepthOnly : SkyboxPipeline);
					renderPass.BindVertexBuffers(skyboxVertexBuffer);
					renderPass.BindIndexBuffer(IndexBuffer, IndexElementSize.ThirtyTwo);
					renderPass.BindFragmentSamplers(new TextureSamplerBinding(SkyboxTexture, SkyboxSampler));
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
						renderPass.BindFragmentSamplers(new TextureSamplerBinding(DepthTexture, DepthSampler));
						renderPass.BindVertexBuffers(BlitVertexBuffer);
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
			GraphicsDevice.WaitForFence(ScreenshotFence);
			GraphicsDevice.ReleaseFence(ScreenshotFence);

			var screenshotSpan = ScreenshotTransferBuffer.Map<Color>(false);
			screenshotPNGBuffer = ImageUtils.EncodePNGBuffer(
				screenshotSpan,
				RenderTexture.Width,
				RenderTexture.Height,
				RenderTexture.Format == TextureFormat.B8G8R8A8Unorm,
				out var size
			);
			ScreenshotTransferBuffer.Unmap();

			if (screenshotPNGBuffer == IntPtr.Zero)
			{
				Logger.LogError("PNG encoding failed!");
				return;
			}

			var commandBuffer = UserStorage.AcquireCommandBuffer();
			screenshotSaveToken = commandBuffer.WriteFile("screenshot.png", screenshotPNGBuffer, (ulong) size);
			UserStorage.Submit(commandBuffer);

			ScreenshotFence = null;
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
