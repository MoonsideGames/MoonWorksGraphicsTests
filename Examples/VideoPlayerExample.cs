using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Video;
using MoonWorks.Input;

namespace MoonWorksGraphicsTests;

class VideoPlayerExample : Example
{
	private VideoAV1 Video;
	private VideoPlayer VideoPlayer;

    public override void Init()
    {
		Window.SetTitle("VideoPlayer");

		// Load the video
		Video = VideoAV1.Create(GraphicsDevice, RootTitleStorage, TestUtils.GetVideoPath("hello.obu"), 25);

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
			cmdbuf.Blit(VideoPlayer.RenderTexture, swapchainTexture, Filter.Linear);
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
