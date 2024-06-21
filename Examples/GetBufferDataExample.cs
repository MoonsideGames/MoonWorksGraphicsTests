using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorksGraphicsTests;

class GetBufferDataExample : Example
{
    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("GetBufferData");

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

		var uploadBuffer = new TransferBuffer(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			vertexBuffer.Size
		);

		var downloadBuffer = new TransferBuffer(
			GraphicsDevice,
			TransferBufferUsage.Download,
			vertexBuffer.Size
		);

		// Read back and print out the vertex values
		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var copyPass = cmdbuf.BeginCopyPass();
		copyPass.DownloadFromBuffer(
			new BufferRegion(vertexBuffer, 0, vertexBuffer.Size),
			new TransferBufferLocation(downloadBuffer)
		);
		cmdbuf.EndCopyPass(copyPass);
		var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		PositionVertex[] readbackVertices = new PositionVertex[vertices.Length];
		downloadBuffer.GetData<PositionVertex>(readbackVertices);
		for (int i = 0; i < readbackVertices.Length; i += 1)
		{
			Logger.LogInfo(readbackVertices[i].ToString());
		}

		// Change the first three vertices and upload
		uploadBuffer.SetData<PositionVertex>(
			readbackVertices,
			false
		);
		uploadBuffer.SetData(otherVerts, false);

		cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer(uploadBuffer, vertexBuffer, false);
		copyPass.DownloadFromBuffer(new BufferRegion(vertexBuffer, 0, vertexBuffer.Size), new TransferBufferLocation(downloadBuffer));
		cmdbuf.EndCopyPass(copyPass);
		fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Read the updated buffer
		downloadBuffer.GetData<PositionVertex>(readbackVertices);
		Logger.LogInfo("=== Change first three vertices ===");
		for (int i = 0; i < readbackVertices.Length; i += 1)
		{
			Logger.LogInfo(readbackVertices[i].ToString());
		}

		// Change the last two vertices and upload
		cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var lastTwoSpan = otherVerts.Slice(1, 2);
		uploadBuffer.SetData(lastTwoSpan, false);
		copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer<PositionVertex>(
			uploadBuffer,
			vertexBuffer,
			0,
			(uint)(vertices.Length - 2),
			2,
			false
		);
		copyPass.DownloadFromBuffer(new BufferRegion(vertexBuffer, 0, vertexBuffer.Size), new TransferBufferLocation(downloadBuffer));
		cmdbuf.EndCopyPass(copyPass);
		fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Read the updated buffer
		downloadBuffer.GetData<PositionVertex>(readbackVertices);
		Logger.LogInfo("=== Change last two vertices ===");
		for (int i = 0; i < readbackVertices.Length; i += 1)
		{
			Logger.LogInfo(readbackVertices[i].ToString());
		}

		vertexBuffer.Dispose();
		uploadBuffer.Dispose();
		downloadBuffer.Dispose();
	}

	public override void Update(System.TimeSpan delta) { }

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			var renderPass = cmdbuf.BeginRenderPass(
				new ColorAttachmentInfo(
					swapchainTexture,
					false,
					Color.Black
				)
			);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
    }
}
