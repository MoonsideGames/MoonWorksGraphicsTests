using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Video;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class VideoPlayerExample : Example
{
	private VideoAV1 Video;
	private VideoPlayer VideoPlayer;

    public override void Init(Window window, GraphicsDevice graphicsDevice, Inputs inputs)
    {
		Window = window;
		GraphicsDevice = graphicsDevice;

		Window.SetTitle("VideoPlayer");

		// Load the video
		Video = new VideoAV1(GraphicsDevice, TestUtils.GetVideoPath("hello.obu"), 25);

		// Play the video
		VideoPlayer = new VideoPlayer(GraphicsDevice);
		VideoPlayer.Load(Video);
		VideoPlayer.Loop = true;
		VideoPlayer.Play();
	}

	public override void Update(System.TimeSpan delta)
	{
		VideoPlayer.Render();
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			cmdbuf.Blit(new BlitInfo
			{
				Source = new BlitRegion
				{
					Texture = VideoPlayer.RenderTexture.Handle,
					W = VideoPlayer.RenderTexture.Width,
					H = VideoPlayer.RenderTexture.Height
				},
				Destination = new BlitRegion
				{
					Texture = swapchainTexture.Handle,
					W = swapchainTexture.Width,
					H = swapchainTexture.Height
				},
				Filter = Filter.Linear
			});
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
       	VideoPlayer.Stop();
		VideoPlayer.Unload();
		VideoPlayer.Dispose();
		Video.Dispose();
    }
}
