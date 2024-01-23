using System;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class SpriteBatchGame : Game
	{
		GraphicsPipeline spriteBatchPipeline;
		Graphics.Buffer quadVertexBuffer;
		Graphics.Buffer quadIndexBuffer;
		SpriteBatch SpriteBatch;

		public unsafe SpriteBatchGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("InstancedSpriteBatch.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("InstancedSpriteBatch.frag"));

			var vertexBufferDescription = VertexBindingAndAttributes.Create<PositionVertex>(0);
			var instanceBufferDescription = VertexBindingAndAttributes.Create<SpriteInstanceData>(1, VertexInputRate.Instance);

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);

			pipelineCreateInfo.VertexInputState = new VertexInputState([
				vertexBufferDescription,
				instanceBufferDescription
			]);

			spriteBatchPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			quadVertexBuffer = Graphics.Buffer.Create<PositionVertex>(GraphicsDevice, BufferUsageFlags.Vertex, 4);
			quadIndexBuffer = Graphics.Buffer.Create<ushort>(GraphicsDevice, BufferUsageFlags.Index, 6);

			var vertices = stackalloc PositionVertex[4];
			vertices[0].Position = new Math.Float.Vector3(0, 0, 0);
			vertices[1].Position = new Math.Float.Vector3(1, 0, 0);
			vertices[2].Position = new Math.Float.Vector3(0, 1, 0);
			vertices[3].Position = new Math.Float.Vector3(1, 1, 0);

			var indices = stackalloc ushort[6]
			{
				0, 1, 2,
				2, 1, 3
			};

			var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.SetBufferData(quadVertexBuffer, new Span<PositionVertex>(vertices, 4));
			cmdbuf.SetBufferData(quadIndexBuffer, new Span<ushort>(indices, 6));
			GraphicsDevice.Submit(cmdbuf);

			SpriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void Update(TimeSpan delta)
		{

		}

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (swapchain != null)
			{
				for (var i = 0; i < 1024; i += 1)
				{
					SpriteBatch.Add()
				}

				SpriteBatch.Upload(cmdbuf);

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchain, Color.Black));
				cmdbuf.BindGraphicsPipeline(spriteBatchPipeline);
				cmdbuf.BindVertexBuffers(
					new BufferBinding(quadVertexBuffer, 0),
					new BufferBinding(SpriteBatch.)
				)
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			SpriteBatchGame game = new SpriteBatchGame();
			game.Run();
		}
	}

	public struct SpriteInstanceData : IVertexType
	{
		public Vector3 Translation;
		public float Rotation;
		public Vector2 Scale;
		public Color Color;
		public Vector2 UV0;
		public Vector2 UV1;
		public Vector2 UV2;
		public Vector2 UV3;

		public static VertexElementFormat[] Formats =>
		[
			VertexElementFormat.Vector3,
			VertexElementFormat.Float,
			VertexElementFormat.Vector2,
			VertexElementFormat.Color,
			VertexElementFormat.Vector2,
			VertexElementFormat.Vector2,
			VertexElementFormat.Vector2,
			VertexElementFormat.Vector2
		];
	}

	class SpriteBatch
	{
		GraphicsDevice GraphicsDevice;
		public Graphics.Buffer BatchBuffer;
		SpriteInstanceData[] InstanceDatas;
		int Index;

		public SpriteBatch(GraphicsDevice graphicsDevice)
		{
			GraphicsDevice = graphicsDevice;
			BatchBuffer = Graphics.Buffer.Create<SpriteInstanceData>(GraphicsDevice, BufferUsageFlags.Vertex, 1024);
			InstanceDatas = new SpriteInstanceData[1024];
			Index = 0;
		}

		public void Add(Vector3 position, float rotation, Vector2 size, Color color)
		{
			InstanceDatas[Index].Translation = position;
			InstanceDatas[Index].Rotation = rotation;
			InstanceDatas[Index].Scale = size;
			InstanceDatas[Index].Color = color;
			InstanceDatas[Index].UV0 = new Vector2(0, 0);
			InstanceDatas[Index].UV1 = new Vector2(0, 1);
			InstanceDatas[Index].UV2 = new Vector2(1, 0);
			InstanceDatas[Index].UV3 = new Vector2(1, 1);
			Index += 1;
		}

		public void Upload(CommandBuffer commandBuffer)
		{
			commandBuffer.SetBufferData(BatchBuffer, InstanceDatas, 0, 0, (uint) Index);
			Index = 0;
		}
	}
}
