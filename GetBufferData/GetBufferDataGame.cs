using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
	class GetBufferDataGame : Game
	{
		public GetBufferDataGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
		{
			var vertices = new System.Span<PositionVertex>(
            [
                new PositionVertex(new Vector3(0, 0, 0)),
				new PositionVertex(new Vector3(0, 0, 1)),
				new PositionVertex(new Vector3(0, 1, 0)),
				new PositionVertex(new Vector3(0, 1, 1)),
				new PositionVertex(new Vector3(1, 0, 0)),
				new PositionVertex(new Vector3(1, 0, 1)),
				new PositionVertex(new Vector3(1, 1, 0)),
				new PositionVertex(new Vector3(1, 1, 1)),
			]);

			var otherVerts = new System.Span<PositionVertex>(
            [
                new PositionVertex(new Vector3(1, 2, 3)),
				new PositionVertex(new Vector3(4, 5, 6)),
				new PositionVertex(new Vector3(7, 8, 9))
			]);

			int vertexSize = Marshal.SizeOf<PositionVertex>();

			var resourceUploader = new ResourceUploader(GraphicsDevice);

			var vertexBuffer = resourceUploader.CreateBuffer(vertices, BufferUsageFlags.Vertex);

			resourceUploader.Upload();
			resourceUploader.Dispose();

			var transferBuffer = new TransferBuffer(GraphicsDevice, vertexBuffer.Size);

			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();

			cmdbuf.BeginCopyPass();
			cmdbuf.DownloadFromBuffer(vertexBuffer, transferBuffer, TransferOptions.Overwrite);
			cmdbuf.EndCopyPass();

			var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);

			// Wait for the vertices to finish copying...
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Read back and print out the vertex values
			PositionVertex[] readbackVertices = new PositionVertex[vertices.Length];
			transferBuffer.GetData<PositionVertex>(readbackVertices);
			for (int i = 0; i < readbackVertices.Length; i += 1)
			{
				Logger.LogInfo(readbackVertices[i].ToString());
			}

			// Change the first three vertices and upload
			cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			transferBuffer.SetData(otherVerts, TransferOptions.Overwrite);
			cmdbuf.BeginCopyPass();
			cmdbuf.UploadToBuffer(transferBuffer, vertexBuffer, WriteOptions.SafeOverwrite);
			cmdbuf.EndCopyPass();
			fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Download the data
			cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.BeginCopyPass();
			cmdbuf.DownloadFromBuffer(vertexBuffer, transferBuffer, TransferOptions.Overwrite);
			cmdbuf.EndCopyPass();
			fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Read the updated buffer
			transferBuffer.GetData<PositionVertex>(readbackVertices);
			Logger.LogInfo("=== Change first three vertices ===");
			for (int i = 0; i < readbackVertices.Length; i += 1)
			{
				Logger.LogInfo(readbackVertices[i].ToString());
			}

			// Change the last two vertices and upload
			cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			var lastTwoSpan = otherVerts.Slice(1, 2);
			transferBuffer.SetData(lastTwoSpan, TransferOptions.Overwrite);
			cmdbuf.BeginCopyPass();
			cmdbuf.UploadToBuffer<PositionVertex>(
				transferBuffer,
				vertexBuffer,
				0,
                (uint)(vertices.Length - 2),
				2,
				WriteOptions.SafeOverwrite
			);
			cmdbuf.EndCopyPass();
			fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			cmdbuf.BeginCopyPass();
			cmdbuf.DownloadFromBuffer(vertexBuffer, transferBuffer, TransferOptions.Overwrite);
			cmdbuf.EndCopyPass();
			fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
			GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

			// Read the updated buffer
			transferBuffer.GetData<PositionVertex>(readbackVertices);
			Logger.LogInfo("=== Change last two vertices ===");
			for (int i = 0; i < readbackVertices.Length; i += 1)
			{
				Logger.LogInfo(readbackVertices[i].ToString());
			}
		}

		protected override void Update(System.TimeSpan delta) { }

		protected override void Draw(double alpha)
		{
			CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
			Texture? swapchainTexture = cmdbuf.AcquireSwapchainTexture(MainWindow);
			if (swapchainTexture != null)
			{
				cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchainTexture, WriteOptions.SafeDiscard, Color.Black));
				cmdbuf.EndRenderPass();
			}
			GraphicsDevice.Submit(cmdbuf);
		}

		public static void Main(string[] args)
		{
			GetBufferDataGame game = new GetBufferDataGame();
			game.Run();
		}
	}
}
