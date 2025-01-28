using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Storage;

namespace MoonWorksGraphicsTests;

public abstract class Example
{
	protected Window Window;
	public GraphicsDevice GraphicsDevice;
	public Inputs Inputs;
	public TitleStorage RootTitleStorage;

	public void Assign(Game game)
	{
		Window = game.MainWindow;
		GraphicsDevice = game.GraphicsDevice;
		Inputs = game.Inputs;
		RootTitleStorage = game.RootTitleStorage;
	}

	public void Start(Game game)
	{
		Assign(game);
		Init();
	}

	public abstract void Init();
	public abstract void Update(TimeSpan delta);
	public abstract void Draw(double alpha);
	public abstract void Destroy();
}
