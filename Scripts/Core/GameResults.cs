namespace MemoryGame.Core;

/// <param name="DigitChange">How the digit count changed after this answer: +1, -1 or 0.</param>
public readonly record struct AnswerResult(
	bool IsCorrect,
	string Expected,
	string Entered,
	int Points,
	float AnswerTime,
	int DigitChange,
	int Digits);

public sealed record GameResult(int Score, int Correct, int Attempts, float TotalCorrectAnswerTime)
{
	public float? AverageAnswerTime => Correct > 0 ? TotalCorrectAnswerTime / Correct : null;
}
