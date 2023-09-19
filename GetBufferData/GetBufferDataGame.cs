using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
    class GetBufferDataGame : Game
    {
        public GetBufferDataGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            PositionVertex[] vertices = new PositionVertex[]
            {
                new PositionVertex(new Vector3(0, 0, 0)),
                new PositionVertex(new Vector3(0, 0, 1)),
                new PositionVertex(new Vector3(0, 1, 0)),
                new PositionVertex(new Vector3(0, 1, 1)),
                new PositionVertex(new Vector3(1, 0, 0)),
                new PositionVertex(new Vector3(1, 0, 1)),
                new PositionVertex(new Vector3(1, 1, 0)),
                new PositionVertex(new Vector3(1, 1, 1)),
            };

            PositionVertex[] otherVerts = new PositionVertex[]
            {
                new PositionVertex(new Vector3(1, 2, 3)),
                new PositionVertex(new Vector3(4, 5, 6)),
                new PositionVertex(new Vector3(7, 8, 9))
            };

            int vertexSize = Marshal.SizeOf<PositionVertex>();

            Buffer vertexBuffer = Buffer.Create<PositionVertex>(
                GraphicsDevice,
                BufferUsageFlags.Vertex,
                (uint) vertices.Length
            );

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(vertexBuffer, vertices);
            var fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);

            // Wait for the vertices to finish uploading...
            GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

            // Read back and print out the vertex values
            PositionVertex[] readbackVertices = new PositionVertex[vertices.Length];
            vertexBuffer.GetData(readbackVertices);
            for (int i = 0; i < readbackVertices.Length; i += 1)
            {
                Logger.LogInfo(readbackVertices[i].ToString());
            }

            // Change the first three vertices
            cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(vertexBuffer, otherVerts);
            fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
            GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

            // Read the updated buffer
            vertexBuffer.GetData(readbackVertices);
            Logger.LogInfo("=== Change first three vertices ===");
            for (int i = 0; i < readbackVertices.Length; i += 1)
            {
                Logger.LogInfo(readbackVertices[i].ToString());
            }

            // Change the last two vertices
            cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(
                vertexBuffer,
                otherVerts,
                (uint) (vertexSize * (vertices.Length - 2)),
                1,
                2
            );
            fence = GraphicsDevice.SubmitAndAcquireFence(cmdbuf);
            GraphicsDevice.WaitForFences(fence);
			GraphicsDevice.ReleaseFence(fence);

            // Read the updated buffer
            vertexBuffer.GetData(readbackVertices);
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
                cmdbuf.BeginRenderPass(new ColorAttachmentInfo(swapchainTexture, Color.Black));
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
