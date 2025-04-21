using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Video;

namespace MoonWorksGraphicsTests;

class VideoPlayerExample : Example
{
	private VideoAV1 Video;

    public override void Init()
    {
		Window.SetTitle("VideoPlayer");

		Video ??= VideoAV1.Create(GraphicsDevice, VideoDevice, RootTitleStorage, TestUtils.GetVideoPath("hello.obu"), 25);

		// Load the video
		Video.Load(true);
		Video.Play();
	}

	public override void Update(System.TimeSpan delta)
	{
		Video.Update(delta);
	}

	public override void Draw(double alpha)
	{
		CommandBuffer cmdbuf = GraphicsDevice.AcquireCommandBuffer();
		Texture swapchainTexture = cmdbuf.AcquireSwapchainTexture(Window);
		if (swapchainTexture != null)
		{
			cmdbuf.Blit(Video.RenderTexture, swapchainTexture, Filter.Linear);
		}
		GraphicsDevice.Submit(cmdbuf);
	}

    public override void Destroy()
    {
		Video.Unload();
    }
}
