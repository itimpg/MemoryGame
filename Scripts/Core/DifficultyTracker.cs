using System;

namespace MemoryGame.Core;

/// <summary>Adds a digit after a streak of right answers and removes one after a streak of wrong answers.</summary>
public sealed class DifficultyTracker
{
	private readonly GameRules _rules;
	private int _correctStreak;
	private int _wrongStreak;

	public DifficultyTracker(GameRules rules)
	{
		_rules = rules;
		Digits = rules.MinDigits;
	}

	public int Digits { get; private set; }

	/// <summary>Records an answer and returns how the digit count changed: +1, -1 or 0.</summary>
	public int RecordAnswer(bool isCorrect)
	{
		if (isCorrect)
		{
			_wrongStreak = 0;
			if (++_correctStreak < _rules.StreakToChangeDigits)
				return 0;
			_correctStreak = 0;
			return ChangeDigits(+1);
		}

		_correctStreak = 0;
		if (++_wrongStreak < _rules.StreakToChangeDigits)
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
