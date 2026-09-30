using Godot;
using MemoryGame.Core;
using MemoryGame.Progress;

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

	public void Display(Run run, RunReward reward, PlayerProfile profile)
	{
		string rewardText = "";
		if (reward.UnlockedSkill is { } skill)
			rewardText += $"New skill unlocked: {skill.Name}!\n{skill.Description}\n\n";
		else if (profile.NextLockedSkill is { } next)
			rewardText += $"Clear stage {next.UnlockAtStage} in a run to unlock a new skill.\n\n";
		if (reward.IsNewBest)
			rewardText += "NEW BEST STAGE!\n\n";

		var stats = run.Stats;
		string average = stats.AverageAnswerTime is { } avg ? $"{avg:0.00}s" : "—";

		_summaryLabel.Text =
			rewardText +
			$"Reached stage {run.Stage}\n" +
			$"Monsters defeated: {stats.StagesCleared}\n" +
			$"Total damage: {stats.TotalDamage}\n" +
			$"Correct: {stats.Correct} / {stats.Attempts}\n" +
			$"Average answer time: {average}\n" +
			$"Upgrades: {UpgradeText.Summary(run.Upgrades)}";
	}
}
