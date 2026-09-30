using Godot;
using MemoryGame.Core;

namespace MemoryGame.UI;

public partial class GameOverScreen : VBoxContainer
{
	[Signal]
	public delegate void PlayAgainRequestedEventHandler();

	[Signal]
	public delegate void MenuRequestedEventHandler();

	private Label _summaryLabel = null!;

	public override void _Ready()
	{
		_summaryLabel = GetNode<Label>("%SummaryLabel");
		GetNode<Button>("%PlayAgainButton").Pressed += () => EmitSignal(SignalName.PlayAgainRequested);
		GetNode<Button>("%MenuButton").Pressed += () => EmitSignal(SignalName.MenuRequested);
	}

	/// <param name="rank">1-based high score rank, or null if the score didn't make the table.</param>
	public void Display(GameResult result, int? rank)
	{
		string rankText = rank switch
		{
			1 => "NEW HIGH SCORE!\n\n",
			{ } r => $"You placed #{r} on the high scores!\n\n",
			null => "",
		};
		string average = result.AverageAnswerTime is { } avg ? $"{avg:0.00}s" : "—";

		_summaryLabel.Text =
			rankText +
			$"Final score: {result.Score}\n" +
			$"Correct: {result.Correct} / {result.Attempts}\n" +
			$"Average answer time: {average}";
	}
}
