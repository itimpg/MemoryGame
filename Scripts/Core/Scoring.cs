using System;

namespace MemoryGame.Core;

public static class Scoring
{
	/// <summary>
	/// Points for a correct answer: each digit is worth <see cref="GameRules.PointsPerDigit"/>
	/// plus a speed bonus that shrinks the longer the player takes.
	/// </summary>
	public static int PointsFor(int digits, float answerTime, GameRules rules)
	{
		float speedBonus = Math.Max(0f, rules.MaxSpeedBonusPerDigit - answerTime * rules.SpeedBonusDecayPerSecond);
		return (int)MathF.Round(digits * (rules.PointsPerDigit + speedBonus));
	}
}
