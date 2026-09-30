using System;

namespace MemoryGame.Core;

/// <summary>
/// Adds a digit after a streak of right answers and removes one after a streak of wrong answers.
/// Each digit gained above the starting count makes the next one exponentially harder to earn.
/// </summary>
public sealed class DifficultyTracker
{
	private readonly GameRules _rules;
	private readonly int _startingDigits;
	private int _wrongStreak;

	public DifficultyTracker(GameRules rules, int startingDigits)
	{
		_rules = rules;
		_startingDigits = Math.Clamp(startingDigits, rules.MinDigits, rules.MaxDigits);
		Digits = _startingDigits;
	}

	public int Digits { get; private set; }

	/// <summary>Right answers in a row so far toward the next digit.</summary>
	public int CorrectStreak { get; private set; }

	/// <summary>Right answers in a row currently needed to add a digit.</summary>
	public int CorrectStreakNeeded
	{
		get
		{
			int gained = Math.Max(0, Digits - _startingDigits);
			return (int)MathF.Ceiling(_rules.CorrectStreakToAddDigit * MathF.Pow(_rules.CorrectStreakGrowth, gained));
		}
	}

	/// <summary>Records an answer and returns how the digit count changed: +1, -1 or 0.</summary>
	public int RecordAnswer(bool isCorrect)
	{
		if (isCorrect)
		{
			_wrongStreak = 0;
			if (++CorrectStreak < CorrectStreakNeeded)
				return 0;
			CorrectStreak = 0;
			return ChangeDigits(+1);
		}

		CorrectStreak = 0;
		if (++_wrongStreak < _rules.WrongStreakToRemoveDigit)
			return 0;
		_wrongStreak = 0;
		return ChangeDigits(-1);
	}

	private int ChangeDigits(int delta)
	{
		int previous = Digits;
		Digits = Math.Clamp(Digits + delta, _rules.MinDigits, _rules.MaxDigits);
		return Digits - previous;
	}
}
