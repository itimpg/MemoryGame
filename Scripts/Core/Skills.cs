using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryGame.Core;

/// <summary>
/// An active skill: equipped before a run, usable once per battle.
/// Unlocked the first time a run ends having cleared <see cref="UnlockAtStage"/> stages.
/// </summary>
/// <param name="NeedsInput">Only usable while the player is entering the number.</param>
public sealed record Skill(string Id, string Name, string Description, int UnlockAtStage, bool NeedsInput = false);

/// <summary>Every skill in the game, in unlock order, and the numbers behind them.</summary>
public static class SkillCatalog
{
	public const int MaxEquipped = 2;

	public const float StrikeHealthFraction = 0.15f;
	public const float ReplaySeconds = 1f;
	public const float TimeStopSeconds = 5f;
	public const float DoubleStrikeMultiplier = 2f;
	public const float SecondWindSeconds = 10f;

	public static readonly Skill Strike = new(
		"strike", "Strike", $"Instantly deal {StrikeHealthFraction * 100:0}% of the monster's max HP.", 2);

	public static readonly Skill Replay = new(
		"replay", "Replay", $"Show the number again for {ReplaySeconds:0.#}s while you're entering it.", 3, NeedsInput: true);

	public static readonly Skill TimeStop = new(
		"time_stop", "Time Stop", $"Freeze the stage clock for {TimeStopSeconds:0} seconds.", 4);

	public static readonly Skill DoubleStrike = new(
		"double_strike", "Double Strike", "Your next correct answer deals double damage.", 5);

	public static readonly Skill SecondWind = new(
		"second_wind", "Second Wind", $"+{SecondWindSeconds:0} seconds on the stage clock.", 6);

	public static readonly Skill Skip = new(
		"skip", "Skip", "Swap the number you're entering for a new one, with no penalty.", 7, NeedsInput: true);

	public static IReadOnlyList<Skill> All { get; } = [Strike, Replay, TimeStop, DoubleStrike, SecondWind, Skip];

	public static Skill ById(string id) =>
		All.FirstOrDefault(s => s.Id == id) ?? throw new ArgumentException($"Unknown skill '{id}'.", nameof(id));
}
