using Godot;

namespace MemoryGame.UI;

/// <summary>
/// Full-screen overlay shown while the scene tree is paused. Its process mode is WhenPaused,
/// so it keeps handling input while the game underneath is frozen.
/// </summary>
public partial class PauseMenu : Control
{
	[Signal]
	public delegate void ResumeRequestedEventHandler();

	[Signal]
	public delegate void MainMenuRequestedEventHandler();

	public override void _Ready()
	{
		GetNode<Button>("%ResumeButton").Pressed += () => EmitSignal(SignalName.ResumeRequested);
		GetNode<Button>("%MainMenuButton").Pressed += () => EmitSignal(SignalName.MainMenuRequested);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Visible && @event.IsActionPressed("ui_cancel"))
		{
			EmitSignal(SignalName.ResumeRequested);
			GetViewport().SetInputAsHandled();
		}
	}
}
