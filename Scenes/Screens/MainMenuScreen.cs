using Godot;
using MemoryGame.Core;

namespace MemoryGame.UI;

public partial class MainMenuScreen : VBoxContainer
{
	[Signal]
	public delegate void StartRequestedEventHandler();

	[Signal]
	public delegate void BestRunsRequestedEventHandler();

	public override void _Ready()
	{
		var rules = GameRules.Default;
		// Built from the rules so the text stays correct when they're tuned.
		GetNode<Label>("%RulesLabel").Text =
			$"A number appears for {rules.ShowDuration:0.#} seconds. Tap it back in to attack the monster — " +
			"longer numbers and faster answers hit harder.\n\n" +
			$"Get it wrong and the monster strikes back: you lose {rules.WrongAnswerTimePenalty:0} seconds.\n\n" +
			$"Defeat each monster within {rules.StageDuration:0} seconds to reach the next stage, then pick an upgrade. " +
			$"Every stage the numbers get longer, up to {rules.MaxDigits} digits — then they flash by faster. How far can you go?";

		GetNode<Button>("%StartButton").Pressed += () => EmitSignal(SignalName.StartRequested);
		GetNode<Button>("%BestRunsButton").Pressed += () => EmitSignal(SignalName.BestRunsRequested);
	}
}
