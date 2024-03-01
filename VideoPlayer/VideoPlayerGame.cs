using MoonWorks.Math.Float;
using MoonWorks.Graphics;
using MoonWorks.Video;

namespace MoonWorks.Test
{
	class VideoPlayerGame : Game
	{
		private GraphicsPipeline pipeline;
		private Sampler sampler;
		private GpuBuffer vertexBuffer;
		private GpuBuffer indexBuffer;

		private Video.VideoAV1 video;
		private VideoPlayer videoPlayer;

		public VideoPlayerGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			// Load the shaders
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("TexturedQuad.frag"));

			// Create the graphics pipeline
			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);
			pipelineCreateInfo.VertexInputState = VertexInputState.CreateSingleBinding<PositionTextureVertex>();
			pipelineCreateInfo.FragmentShaderInfo.SamplerBindingCount = 1;
			pipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			// Create the sampler
			sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.LinearClamp);

			// Create and populate the GPU resources
			var resourceUploader = new ResourceUploader(GraphicsDevice);

			vertexBuffer = resourceUploader.CreateBuffer(
				[
					new PositionTextureVertex(new Vector3(-1, -1, 0), new Vector2(0, 0)),
					new PositionTextureVertex(new Vector3(1, -1, 0), new Vector2(1, 0)),
					new PositionTextureVertex(new Vector3(1, 1, 0), new Vector2(1, 1)),
					new PositionTextureVertex(new Vector3(-1, 1, 0), new Vector2(0, 1)),
				],
				BufferUsageFlags.Vertex
			);

			indexBuffer = resourceUploader.CreateBuffer<ushort>(
				[
					0, 1, 2,
					0, 2, 3,
				],
				BufferUsageFlags.Index
			);

			resourceUploader.Upload();
			resourceUploader.Dispose();

			// Load the video
			video = new VideoAV1(GraphicsDevice, TestUtils.GetVideoPath("hello.obu"), 25);

			// Play the video
			videoPlayer = new VideoPlayer(GraphicsDevice);
			videoPlayer.Load(video);
			videoPlayer.Loop = true;
			videoPlayer.Play();
		}

		protected override void Update(System.TimeSpan delta)
		{
			videoPlayer.Render();
		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (backbuffer != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(backbuffer, WriteOptions.SafeDiscard, Color.CornflowerBlue));
				cmdbuf.BindGraphicsPipeline(pipeline);
				cmdbuf.BindVertexBuffers(vertexBuffer);
				cmdbuf.BindIndexBuffer(indexBuffer, IndexElementSize.Sixteen);
				cmdbuf.BindFragmentSamplers(new TextureSamplerBinding(videoPlayer.RenderTexture, sampler));
				cmdbuf.DrawIndexedPrimitives(0, 0, 2);
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			VideoPlayerGame game = new VideoPlayerGame();
			game.Run();
		}
	}
}
