using System;
using MoonWorks.Graphics;
using MoonWorks.Math;
using MoonWorks.Math.Float;

namespace MoonWorks.Test
{
	class SpriteBatchGame : Game
	{
		GraphicsPipeline spriteBatchPipeline;
		Graphics.Buffer quadVertexBuffer;
		Graphics.Buffer quadIndexBuffer;
		SpriteBatch SpriteBatch;
		Texture Texture;
		Sampler Sampler;

		Matrix4x4 View;
		Matrix4x4 Projection;

		Random Random;

		public unsafe SpriteBatchGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			Random = new Random();

			ShaderModule vertShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("InstancedSpriteBatch.vert"));
			ShaderModule fragShaderModule = new ShaderModule(GraphicsDevice, TestUtils.GetShaderPath("InstancedSpriteBatch.frag"));

			var vertexBufferDescription = VertexBindingAndAttributes.Create<PositionVertex>(0);
			var instanceBufferDescription = VertexBindingAndAttributes.Create<SpriteInstanceData>(1, 1, VertexInputRate.Instance);

			GraphicsPipelineCreateInfo pipelineCreateInfo = TestUtils.GetStandardGraphicsPipelineCreateInfo(
				MainWindow.SwapchainFormat,
				vertShaderModule,
				fragShaderModule
			);

			pipelineCreateInfo.VertexShaderInfo = GraphicsShaderInfo.Create<ViewProjectionMatrices>(vertShaderModule, "main", 0);
			pipelineCreateInfo.FragmentShaderInfo = GraphicsShaderInfo.Create(fragShaderModule, "main", 1);

			pipelineCreateInfo.VertexInputState = new VertexInputState([
				vertexBufferDescription,
				instanceBufferDescription
			]);

			spriteBatchPipeline = new GraphicsPipeline(GraphicsDevice, pipelineCreateInfo);

			Texture = Texture.CreateTexture2D(GraphicsDevice, 1, 1, TextureFormat.R8G8B8A8, TextureUsageFlags.Sampler);
			Sampler = new Sampler(GraphicsDevice, SamplerCreateInfo.PointClamp);

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
			cmdbuf.SetTextureData(Texture, new Color[1] { Color.White });
			GraphicsDevice.Submit(cmdbuf);

			SpriteBatch = new SpriteBatch(GraphicsDevice);

			// View = Matrix4x4.CreateLookAt(
			// 	new Vector3(0, 0, -1),
			// 	Vector3.Zero,
			// 	Vector3.Up
			// );

			//View = Matrix4x4.Identity;

			View = Matrix4x4.CreateLookAt(
				new Vector3(0, 0, 1),
				Vector3.Zero,
				Vector3.Up
			);

			Projection = Matrix4x4.CreateOrthographicOffCenter(
				0,
				MainWindow.Width,
				MainWindow.Height,
				0,
				0.01f,
				10
			);

			// Projection = Matrix4x4.CreatePerspectiveFieldOfView(
			// 	MathHelper.ToRadians(75f),
			// 	(float) MainWindow.Width / MainWindow.Height,
			// 	0.01f,
			// 	1000
			// );
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
				SpriteBatch.Reset();

				for (var i = 0; i < 1024; i += 1)
				{
					var position = new Vector3(Random.Next((int) MainWindow.Width), Random.Next((int) MainWindow.Height), 1);
					SpriteBatch.Add(
						position,
						0f,
						new Vector2(100, 100),
						new Color(Random.Next(255), Random.Next(255), Random.Next(255)),
						new Vector2(0, 0),
						new Vector2(1, 1)
					);
				}

				SpriteBatch.Upload(cmdbuf);

				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchain, Color.Black));
				SpriteBatch.Render(cmdbuf, spriteBatchPipeline, Texture, Sampler, quadVertexBuffer, quadIndexBuffer, new ViewProjectionMatrices(View, Projection));
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

	public readonly record struct ViewProjectionMatrices(Matrix4x4 View, Matrix4x4 Projection);

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
		uint Index;

		public uint InstanceCount => Index;

		public SpriteBatch(GraphicsDevice graphicsDevice)
		{
			GraphicsDevice = graphicsDevice;
			BatchBuffer = Graphics.Buffer.Create<SpriteInstanceData>(GraphicsDevice, BufferUsageFlags.Vertex, 1024);
			InstanceDatas = new SpriteInstanceData[1024];
			Index = 0;
		}

		public void Reset()
		{
			Index = 0;
		}

		public void Add(
			Vector3 position,
			float rotation,
			Vector2 size,
			Color color,
			Vector2 leftTopUV,
			Vector2 dimensionsUV
		) {
			var left = leftTopUV.X;
			var top = leftTopUV.Y;
			var right = leftTopUV.X + dimensionsUV.X;
			var bottom = leftTopUV.Y + dimensionsUV.Y;

			InstanceDatas[Index].Translation = position;
			InstanceDatas[Index].Rotation = rotation;
			InstanceDatas[Index].Scale = size;
			InstanceDatas[Index].Color = color;
			InstanceDatas[Index].UV0 = leftTopUV;
			InstanceDatas[Index].UV1 = new Vector2(left, bottom);
			InstanceDatas[Index].UV2 = new Vector2(right, top);
			InstanceDatas[Index].UV3 = new Vector2(right, bottom);
			Index += 1;
		}

		public void Upload(CommandBuffer commandBuffer)
		{
			commandBuffer.SetBufferData(BatchBuffer, InstanceDatas, 0, 0, (uint) Index);
		}

		public void Render(CommandBuffer commandBuffer, GraphicsPipeline pipeline, Texture texture, Sampler sampler, Graphics.Buffer quadVertexBuffer, Graphics.Buffer quadIndexBuffer, ViewProjectionMatrices viewProjectionMatrices)
		{
			commandBuffer.BindGraphicsPipeline(pipeline);
			commandBuffer.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
			commandBuffer.BindVertexBuffers(
				new BufferBinding(quadVertexBuffer, 0),
				new BufferBinding(BatchBuffer, 0)
			);
			commandBuffer.BindIndexBuffer(quadIndexBuffer, IndexElementSize.Sixteen);
			var vertParamOffset = commandBuffer.PushVertexShaderUniforms(viewProjectionMatrices);
			commandBuffer.DrawInstancedPrimitives(0, 0, 2, InstanceCount, vertParamOffset, 0);

		}
	}
}
