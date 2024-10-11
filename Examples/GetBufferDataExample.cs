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

		var uploadBuffer = TransferBuffer.Create<byte>(
			GraphicsDevice,
			TransferBufferUsage.Upload,
			vertexBuffer.Size
		);

		var downloadBuffer = TransferBuffer.Create<byte>(
			GraphicsDevice,
			TransferBufferUsage.Download,
			vertexBuffer.Size
		);

		// Read back and print out the vertex values
		var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		var copyPass = cmdbuf.BeginCopyPass();
		copyPass.DownloadFromBuffer(
			new BufferRegion
			{
				Buffer = vertexBuffer.Handle,
				Size = vertexBuffer.Size
			},
			new TransferBufferLocation
			{
				TransferBuffer = downloadBuffer.Handle
			}
		);
		cmdbuf.EndCopyPass(copyPass);
		var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		var readbackVertices = downloadBuffer.Map<PositionVertex>(false);
		for (int i = 0; i < readbackVertices.Length; i += 1)
		{
			Logger.LogInfo(readbackVertices[i].ToString());
		}

		// Change the first three vertices and upload
		var uploadVertices = uploadBuffer.Map<PositionVertex>(false);
		readbackVertices.CopyTo(uploadVertices);
		otherVerts.CopyTo(uploadVertices);
		downloadBuffer.Unmap();
		uploadBuffer.Unmap();

		cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer(uploadBuffer, vertexBuffer, false);
		copyPass.DownloadFromBuffer(
			new BufferRegion
			{
				Buffer = vertexBuffer.Handle,
				Size = vertexBuffer.Size
			},
			new TransferBufferLocation
			{
				TransferBuffer = downloadBuffer.Handle
			}
		);
		cmdbuf.EndCopyPass(copyPass);
		fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Read the updated buffer
		readbackVertices = downloadBuffer.Map<PositionVertex>(false);
		Logger.LogInfo("=== Change first three vertices ===");
		for (int i = 0; i < readbackVertices.Length; i += 1)
		{
			Logger.LogInfo(readbackVertices[i].ToString());
		}
		downloadBuffer.Unmap();

		// Change the last two vertices and upload
		uploadVertices = uploadBuffer.Map<PositionVertex>(false);
		var lastTwoSpan = otherVerts.Slice(1, 2);
		lastTwoSpan.CopyTo(uploadVertices.Slice(uploadVertices.Length - 3));
		uploadBuffer.Unmap();

		cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		copyPass = cmdbuf.BeginCopyPass();
		copyPass.UploadToBuffer<PositionVertex>(
			uploadBuffer,
			vertexBuffer,
			0,
			(uint)(vertices.Length - 2),
			2,
			false
		);
		copyPass.DownloadFromBuffer(
			new BufferRegion
			{
				Buffer = vertexBuffer.Handle,
				Size = vertexBuffer.Size
			},
			new TransferBufferLocation
			{
				TransferBuffer = downloadBuffer.Handle
			}
		);
		cmdbuf.EndCopyPass(copyPass);
		fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
		GraphicsDevice.WaitForFence(fence);
		GraphicsDevice.ReleaseFence(fence);

		// Read the updated buffer
		readbackVertices = downloadBuffer.Map<PositionVertex>(false);
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
				new ColorTargetInfo
				{
					Texture = swapchainTexture.Handle,
					LoadOp = LoadOp.Clear,
					ClearColor = Color.Black
				}
			);
			cmdbuf.EndRenderPass(renderPass);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
    }
}
