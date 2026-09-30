namespace MemoryGame.Core;

/// <summary>Tunable values for a run. All times are in seconds.</summary>
public sealed record GameRules
{
	public static GameRules Default { get; } = new();

	public float StageDuration { get; init; } = 60f;
	public float FeedbackDuration { get; init; } = 0.6f;

	/// <summary>Time lost when the monster strikes back after a wrong answer.</summary>
	public float WrongAnswerTimePenalty { get; init; } = 3f;

	public int MinDigits { get; init; } = 2;
	public int MaxDigits { get; init; } = 7;

	// Stage 1 starts at StartingDigits; each later stage starts DigitsAddedPerStage longer, up to MaxDigits.
	public int StartingDigits { get; init; } = 2;
	public int DigitsAddedPerStage { get; init; } = 1;

	// How long the number is shown. Once stages start at MaxDigits, each further stage shows it
	// ShowDurationMultiplierPerStage times as long as the stage before (exponential decay), down to MinShowDuration.
	public float ShowDuration { get; init; } = 2f;
	public float MinShowDuration { get; init; } = 0.5f;
	public float ShowDurationMultiplierPerStage { get; init; } = 0.8f;

	// Right answers in a row needed to add a digit grow exponentially with how many digits the
	// player has already gained this stage: CorrectStreakToAddDigit × CorrectStreakGrowth^gained (2, 4, 8, …).
	public int CorrectStreakToAddDigit { get; init; } = 2;
	public float CorrectStreakGrowth { get; init; } = 2f;

	/// <summary>Wrong answers in a row needed to remove a digit.</summary>
	public int WrongStreakToRemoveDigit { get; init; } = 2;

	// Damage is per digit, so longer numbers hit harder.
	public int DamagePerDigit { get; init; } = 10;
	public float MaxSpeedBonusPerDigit { get; init; } = 10f;
	public float SpeedBonusDecayPerSecond { get; init; } = 2f;

	/// <summary>Stage 1 monster health; each later stage multiplies it by <see cref="MonsterHealthGrowth"/>.</summary>
	public int MonsterBaseHealth { get; init; } = 240;
	public float MonsterHealthGrowth { get; init; } = 1.3f;
}
