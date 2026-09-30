namespace MemoryGame.Core;

/// <param name="Damage">Damage dealt to the monster; 0 for a wrong answer.</param>
/// <param name="DamageMultiplier">Bonus from upgrades that went into <paramref name="Damage"/>; 1 means none.</param>
/// <param name="TimePenalty">Seconds lost to the monster's counterattack; 0 for a correct or forgiven answer.</param>
/// <param name="MistakeForgiven">A wrong answer that Second Chance saved from the time penalty.</param>
/// <param name="DigitChange">How the digit count changed after this answer: +1, -1 or 0.</param>
/// <param name="NumberLength">How long the next numbers will be.</param>
public readonly record struct AnswerResult(
	bool IsCorrect,
	string Expected,
	string Entered,
	int Damage,
	float DamageMultiplier,
	float TimePenalty,
	bool MistakeForgiven,
	float AnswerTime,
	int DigitChange,
	int NumberLength);
