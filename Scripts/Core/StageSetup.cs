using System;

namespace MemoryGame.Core;

/// <summary>
/// What a stage starts with. Each stage starts one digit longer than the last until
/// <see cref="GameRules.MaxDigits"/>; after that the show time shrinks exponentially each stage.
/// </summary>
public sealed record StageSetup(int Stage, Monster Monster, int StartingDigits, float ShowDuration)
{
	public static StageSetup For(int stage, GameRules rules)
	{
		int startingDigits = Math.Min(rules.StartingDigits + (stage - 1) * rules.DigitsAddedPerStage, rules.MaxDigits);

		// Stages after the first one that starts at the max digit count show the number for less time.
		int firstStageAtMaxDigits = 1 + (int)MathF.Ceiling((rules.MaxDigits - rules.StartingDigits) / (float)rules.DigitsAddedPerStage);
		int stagesPastMaxDigits = Math.Max(0, stage - firstStageAtMaxDigits);
		float showDuration = Math.Max(
			rules.MinShowDuration,
			rules.ShowDuration * MathF.Pow(rules.ShowDurationMultiplierPerStage, stagesPastMaxDigits));

		return new StageSetup(stage, Monster.ForStage(stage, rules), startingDigits, showDuration);
	}
}
