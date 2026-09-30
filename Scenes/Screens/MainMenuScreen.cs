using Godot;
using MemoryGame.Core;

namespace MemoryGame.UI;

public partial class MainMenuScreen : VBoxContainer
{
	[Signal]
	public delegate void StartRequestedEventHandler();

	[Signal]
	public delegate void HighScoresRequestedEventHandler();

	public override void _Ready()
	{
		var rules = GameRules.Default;
		// Built from the rules so the text stays correct when they're tuned.
		GetNode<Label>("%RulesLabel").Text =
			$"A number appears for {rules.ShowDuration:0.#} seconds, then disappears.\n\n" +
			"Tap it back in on the number pad — your answer is checked as soon as you enter the last digit. " +
			"Longer numbers and faster answers score more.\n\n" +
			$"{rules.StreakToChangeDigits} right in a row adds a digit, " +
			$"{rules.StreakToChangeDigits} wrong in a row removes one.\n\n" +
			$"You have {rules.GameDuration:0} seconds.";

		GetNode<Button>("%StartButton").Pressed += () => EmitSignal(SignalName.StartRequested);
		GetNode<Button>("%HighScoresButton").Pressed += () => EmitSignal(SignalName.HighScoresRequested);
	}
}
