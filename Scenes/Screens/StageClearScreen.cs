using System.Collections.Generic;
using Godot;
using MemoryGame.Audio;
using MemoryGame.Core;

namespace MemoryGame.UI;

/// <summary>Shown between stages: what the player just beat, what comes next, and a choice of upgrades.</summary>
public partial class StageClearScreen : VBoxContainer
{
	[Signal]
	public delegate void UpgradeChosenEventHandler(string upgradeId);

	/// <summary>Only used when there's nothing left to offer (every upgrade is maxed out).</summary>
	[Signal]
	public delegate void NextStageRequestedEventHandler();

	private Label _titleLabel = null!;
	private Label _summaryLabel = null!;
	private Label _choosePrompt = null!;
	private VBoxContainer _choices = null!;
	private Button _nextButton = null!;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_summaryLabel = GetNode<Label>("%SummaryLabel");
		_choosePrompt = GetNode<Label>("%ChoosePrompt");
		_choices = GetNode<VBoxContainer>("%Choices");
		_nextButton = GetNode<Button>("%NextButton");
		_nextButton.Pressed += () => EmitSignal(SignalName.NextStageRequested);
	}

	public void Display(Run run, IReadOnlyList<Upgrade> offers)
	{
		var next = run.PeekNextStage();
		_titleLabel.Text = $"Stage {run.Stage}\nCleared!";
		_summaryLabel.Text =
			$"Next up: {next.Monster.Name} — {next.Monster.MaxHealth} HP\n" +
			$"{next.StartingDigits + run.Upgrades.ExtraDigits}-digit numbers, " +
			$"shown for {next.ShowDuration + run.Upgrades.ExtraShowTime:0.##}s\n\n" +
			$"Upgrades: {UpgradeText.Summary(run.Upgrades)}";

		foreach (var child in _choices.GetChildren())
			child.QueueFree();
		foreach (var upgrade in offers)
			_choices.AddChild(CreateChoice(upgrade, run.Upgrades.LevelOf(upgrade) + 1));

		bool hasOffers = offers.Count > 0;
		_choosePrompt.Visible = hasOffers;
		_choices.Visible = hasOffers;
		_nextButton.Visible = !hasOffers;
	}

	private Button CreateChoice(Upgrade upgrade, int nextLevel)
	{
		var button = new Button
		{
			Text = $"{UpgradeText.NameWithLevel(upgrade, nextLevel)}\n{upgrade.Description}",
			Icon = ArtLoader.UpgradeIcon(upgrade.Id),
			Alignment = HorizontalAlignment.Left,
			ThemeTypeVariation = "UpgradeButton",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			CustomMinimumSize = new Vector2(0, 130),
		};
		button.AddToGroup(AudioManager.SilentButtonGroup); // Main plays the upgrade sound instead.
		button.Pressed += () => EmitSignal(SignalName.UpgradeChosen, upgrade.Id);
		return button;
	}
}
