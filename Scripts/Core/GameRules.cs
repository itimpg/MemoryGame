namespace MemoryGame.Core;

/// <summary>Tunable values for a game. All times are in seconds.</summary>
public sealed record GameRules
{
	public static GameRules Default { get; } = new();

	public float GameDuration { get; init; } = 60f;
	public float ShowDuration { get; init; } = 2f;
	public float FeedbackDuration { get; init; } = 0.6f;

	public int MinDigits { get; init; } = 3;
	public int MaxDigits { get; init; } = 9;

	/// <summary>Right (or wrong) answers in a row needed to add (or remove) a digit.</summary>
	public int StreakToChangeDigits { get; init; } = 2;

	// Points are per digit, so longer numbers are worth more.
	public int PointsPerDigit { get; init; } = 10;
	public float MaxSpeedBonusPerDigit { get; init; } = 10f;
	public float SpeedBonusDecayPerSecond { get; init; } = 2f;
}
