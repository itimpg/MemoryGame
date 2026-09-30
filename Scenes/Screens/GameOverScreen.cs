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

	/// <param name="rank">1-based best run rank, or null if the run didn't make the table.</param>
	public void Display(Run run, int? rank)
	{
		string rankText = rank switch
		{
			1 => "NEW BEST RUN!\n\n",
			{ } r => $"That's your #{r} best run!\n\n",
			null => "",
		};
		var stats = run.Stats;
		string average = stats.AverageAnswerTime is { } avg ? $"{avg:0.00}s" : "—";

		_summaryLabel.Text =
			rankText +
			$"Reached stage {run.Stage}\n" +
			$"Monsters defeated: {stats.StagesCleared}\n" +
			$"Total damage: {stats.TotalDamage}\n" +
			$"Correct: {stats.Correct} / {stats.Attempts}\n" +
			$"Average answer time: {average}\n" +
			$"Upgrades: {UpgradeText.Summary(run.Upgrades)}";
	}
}
