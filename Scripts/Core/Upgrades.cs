using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryGame.Core;

/// <summary>An upgrade the player can pick after clearing a stage. Some can be taken more than once.</summary>
public sealed record Upgrade(string Id, string Name, string Description, int MaxLevel);

/// <summary>Every upgrade in the game, and the numbers behind them.</summary>
public static class UpgradeCatalog
{
	public const float PowerBonusPerLevel = 0.15f;
	public const float ComboBonusPerAnswer = 0.1f;
	public const int MaxComboAnswers = 10;
	public const float SlowTimeSecondsPerLevel = 0.3f;
	public const float TimeExtendSecondsPerLevel = 5f;
	public const float GambleDamageMultiplier = 1.3f;

	public static readonly Upgrade Power = new(
		"power", "Power Strike", $"+{PowerBonusPerLevel * 100:0}% damage.", 3);

	public static readonly Upgrade Combo = new(
		"combo", "Combo",
		$"Each correct answer in a row adds +{ComboBonusPerAnswer * 100:0}% damage to the next (up to {MaxComboAnswers}). A wrong answer resets it.", 3);

	public static readonly Upgrade Hint = new(
		"hint", "Hint", "The first digit is filled in for you.", 1);

	public static readonly Upgrade SecondChance = new(
		"second_chance", "Second Chance", "One wrong answer per stage costs no time.", 2);

	public static readonly Upgrade SlowTime = new(
		"slow_time", "Slow Time", $"Numbers stay on screen {SlowTimeSecondsPerLevel:0.#}s longer.", 3);

	public static readonly Upgrade TimeExtend = new(
		"time_extend", "Extra Time", $"+{TimeExtendSecondsPerLevel:0} seconds per stage.", 3);

	public static readonly Upgrade Gamble = new(
		"gamble", "Gamble", $"Numbers are 1 digit longer, but hit {(GambleDamageMultiplier - 1) * 100:0}% harder.", 2);

	public static IReadOnlyList<Upgrade> All { get; } = [Power, Combo, Hint, SecondChance, SlowTime, TimeExtend, Gamble];

	public static Upgrade ById(string id) =>
		All.FirstOrDefault(u => u.Id == id) ?? throw new ArgumentException($"Unknown upgrade '{id}'.", nameof(id));
}

/// <summary>The upgrades a run has picked so far, and what they add up to.</summary>
public sealed class UpgradeSet
{
	private readonly Dictionary<string, int> _levels = [];

	public int LevelOf(Upgrade upgrade) => _levels.GetValueOrDefault(upgrade.Id);

	public bool CanTake(Upgrade upgrade) => LevelOf(upgrade) < upgrade.MaxLevel;

	public void Add(Upgrade upgrade)
	{
		if (!CanTake(upgrade))
			throw new InvalidOperationException($"{upgrade.Name} is already at max level.");
		_levels[upgrade.Id] = LevelOf(upgrade) + 1;
	}

	/// <summary>Owned upgrades with their levels, in catalog order.</summary>
	public IEnumerable<(Upgrade Upgrade, int Level)> Owned =>
		UpgradeCatalog.All.Where(u => LevelOf(u) > 0).Select(u => (u, LevelOf(u)));

	/// <param name="combo">Correct answers in a row, including the one being scored.</param>
	public float DamageMultiplier(int combo)
	{
		int comboAnswers = Math.Clamp(combo - 1, 0, UpgradeCatalog.MaxComboAnswers);
		return (1 + UpgradeCatalog.PowerBonusPerLevel * LevelOf(UpgradeCatalog.Power))
			* (1 + UpgradeCatalog.ComboBonusPerAnswer * LevelOf(UpgradeCatalog.Combo) * comboAnswers)
			* MathF.Pow(UpgradeCatalog.GambleDamageMultiplier, LevelOf(UpgradeCatalog.Gamble));
	}

	public float ExtraShowTime => UpgradeCatalog.SlowTimeSecondsPerLevel * LevelOf(UpgradeCatalog.SlowTime);
	public float ExtraStageTime => UpgradeCatalog.TimeExtendSecondsPerLevel * LevelOf(UpgradeCatalog.TimeExtend);
	public int FreeMistakesPerStage => LevelOf(UpgradeCatalog.SecondChance);
	public int ExtraDigits => LevelOf(UpgradeCatalog.Gamble);
	public bool RevealsFirstDigit => LevelOf(UpgradeCatalog.Hint) > 0;
}
