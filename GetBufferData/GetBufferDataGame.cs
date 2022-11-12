using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using System.Runtime.InteropServices;

namespace MoonWorks.Test
{
    internal class GetBufferDataGame : Game
    {
        public GetBufferDataGame() : base(TestUtils.GetStandardWindowCreateInfo(), TestUtils.GetStandardFrameLimiterSettings(), 60, true)
        {
            PositionColorVertex[] vertices = new PositionColorVertex[]
            {
                new PositionColorVertex(new Vector3(0, 0, 0), Color.Red),
                new PositionColorVertex(new Vector3(0, 0, 1), Color.Green),
                new PositionColorVertex(new Vector3(0, 1, 0), Color.Blue),
                new PositionColorVertex(new Vector3(0, 1, 1), Color.Yellow),
                new PositionColorVertex(new Vector3(1, 0, 0), Color.Orange),
                new PositionColorVertex(new Vector3(1, 0, 1), Color.Brown),
                new PositionColorVertex(new Vector3(1, 1, 0), Color.Black),
                new PositionColorVertex(new Vector3(1, 1, 1), Color.White),
            };

            PositionColorVertex[] otherVerts = new PositionColorVertex[]
            {
                new PositionColorVertex(new Vector3(0.5f, 0.5f, 0.5f), Color.Fuchsia),
                new PositionColorVertex(new Vector3(0.1f, 0.1f, 0.1f), Color.LightCoral),
                new PositionColorVertex(new Vector3(0.2f, 0.2f, 0.2f), Color.Lime)
            };

            int vertexSize = Marshal.SizeOf<PositionColorVertex>();

            Buffer vertexBuffer = Buffer.Create<PositionColorVertex>(
                GraphicsDevice,
                BufferUsageFlags.Vertex,
                (uint) vertices.Length
            );

            CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(vertexBuffer, vertices);
            GraphicsDevice.Submit(cmdbuf);

            // Wait for the vertices to finish uploading...
            GraphicsDevice.Wait();

            // Read back and print out the vertex values
            PositionColorVertex[] readbackVertices = new PositionColorVertex[vertices.Length];
            vertexBuffer.GetData(
                readbackVertices,
                (uint) (vertexSize * readbackVertices.Length) // FIXME: Seems like this should get auto-calculated somehow
            );
            for (int i = 0; i < readbackVertices.Length; i += 1)
            {
                Logger.LogInfo(readbackVertices[i].ToString());
            }

            // Change the first three vertices
            cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            cmdbuf.SetBufferData(vertexBuffer, otherVerts);
            GraphicsDevice.Submit(cmdbuf);
            GraphicsDevice.Wait();

            // Read the updated buffer
            vertexBuffer.GetData(
                readbackVertices,
                (uint) (vertexSize * readbackVertices.Length)
            );
            Logger.LogInfo("===");
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
            GraphicsDevice.Submit(cmdbuf);
            GraphicsDevice.Wait();

            // Read the updated buffer
            vertexBuffer.GetData(
                readbackVertices,
                (uint) (vertexSize * readbackVertices.Length)
            );
            Logger.LogInfo("===");
            for (int i = 0; i < readbackVertices.Length; i += 1)
            {
                Logger.LogInfo(readbackVertices[i].ToString());
            }
        }


        protected override void Update(System.TimeSpan delta) { }

        protected override void Draw(double alpha) { }

        public static void Main(string[] args)
        {
            GetBufferDataGame game = new GetBufferDataGame();
            game.Run();
        }
    }
}
