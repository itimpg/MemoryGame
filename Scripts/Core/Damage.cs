using System;

namespace MemoryGame.Core;

public static class Damage
{
	/// <summary>
	/// Damage for a correct answer: each digit deals <see cref="GameRules.DamagePerDigit"/>
	/// plus a speed bonus that shrinks the longer the player takes.
	/// </summary>
	public static int Calculate(int digits, float answerTime, GameRules rules)
	{
		float speedBonus = Math.Max(0f, rules.MaxSpeedBonusPerDigit - answerTime * rules.SpeedBonusDecayPerSecond);
		return (int)MathF.Round(digits * (rules.DamagePerDigit + speedBonus));
	}
}
