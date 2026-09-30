using Godot;
using MemoryGame.Core;
using MemoryGame.Progress;

namespace MemoryGame.UI;

public partial class MainMenuScreen : VBoxContainer
{
	[Signal]
	public delegate void StartRequestedEventHandler();

	/// <summary>Emitted only after the player confirms they want to delete their progress.</summary>
	[Signal]
	public delegate void ClearSaveRequestedEventHandler();

	private Label _progressLabel = null!;
	private Button _clearSaveButton = null!;
	private ConfirmationDialog _clearSaveDialog = null!;

	public override void _Ready()
	{
		var rules = GameRules.Default;
		// Built from the rules so the text stays correct when they're tuned.
		GetNode<Label>("%RulesLabel").Text =
			$"A number appears for {rules.ShowDuration:0.#} seconds. Tap it back in to attack the monster — " +
			"longer numbers and faster answers hit harder.\n\n" +
			$"Get it wrong and the monster strikes back: you lose {rules.WrongAnswerTimePenalty:0} seconds.\n\n" +
			$"Defeat each monster within {rules.StageDuration:0} seconds to reach the next stage, then pick an upgrade. " +
			"Go far enough to unlock skills you can use once per battle.";

		_progressLabel = GetNode<Label>("%ProgressLabel");
		GetNode<Button>("%StartButton").Pressed += () => EmitSignal(SignalName.StartRequested);

		_clearSaveDialog = new ConfirmationDialog
		{
			Title = "Clear save data",
			DialogText = "Delete all progress? Your best stage and every unlocked skill will be lost. This can't be undone.",
			DialogAutowrap = true,
			OkButtonText = "Delete",
			CancelButtonText = "Cancel",
		};
		_clearSaveDialog.Confirmed += () => EmitSignal(SignalName.ClearSaveRequested);
		AddChild(_clearSaveDialog);

		_clearSaveButton = GetNode<Button>("%ClearSaveButton");
		_clearSaveButton.Pressed += () => _clearSaveDialog.PopupCentered(new Vector2I(600, 320));
	}

	public void ShowProgress(PlayerProfile profile)
	{
		bool hasProgress = profile.BestStage > 0 || profile.UnlockedSkills.Count > 0;
		_progressLabel.Text = hasProgress
			? $"Best: stage {profile.BestStage}    Skills: {profile.UnlockedSkills.Count} / {SkillCatalog.All.Count}"
			: "";
		_clearSaveButton.Visible = hasProgress; // Nothing to clear on a fresh save.
	}

	/// <summary>Closes the confirmation dialog if it's open (e.g. for the Android back button). Returns whether it was.</summary>
	public bool CloseDialog()
	{
		if (!_clearSaveDialog.Visible)
			return false;
		_clearSaveDialog.Hide();
		return true;
	}
}
