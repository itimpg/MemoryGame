using System;
using System.Collections.Generic;
using System.Linq;

namespace MemoryGame.Core;

/// <summary>Totals across every stage of a run.</summary>
public sealed class RunStats
{
	public int StagesCleared { get; internal set; }
	public int TotalDamage { get; internal set; }
	public int Correct { get; internal set; }
	public int Attempts { get; internal set; }
	public float TotalCorrectAnswerTime { get; internal set; }

	public float? AverageAnswerTime => Correct > 0 ? TotalCorrectAnswerTime / Correct : null;
}

/// <summary>A series of stages, one monster each, that lasts until the player loses a battle.</summary>
public sealed class Run
{
	private readonly GameRules _rules;
	private readonly Random _rng;

	public Run(GameRules rules, Random rng, IReadOnlyList<Skill>? skills = null)
	{
		_rules = rules;
		_rng = rng;
		Skills = skills ?? [];
	}

	/// <summary>Skills equipped for this run; each battle gets one use of each.</summary>
	public IReadOnlyList<Skill> Skills { get; }

	/// <summary>The current (1-based) stage, or 0 before the first battle.</summary>
	public int Stage { get; private set; }

	public RunStats Stats { get; } = new();

	public UpgradeSet Upgrades { get; } = new();

	public StageSetup PeekNextStage() => StageSetup.For(Stage + 1, _rules);

	public Battle StartNextStage()
	{
		Stage++;
		return new Battle(_rules, _rng, StageSetup.For(Stage, _rules), Stats, Upgrades, Skills);
	}

	/// <summary>
	/// Picks up to <paramref name="count"/> different upgrades from <paramref name="pool"/> that aren't maxed out yet.
	/// The pool is passed in so it can later be limited to what the player has unlocked.
	/// </summary>
	public IReadOnlyList<Upgrade> RollUpgradeOffer(IReadOnlyList<Upgrade> pool, int count = 3) =>
		pool.Where(Upgrades.CanTake).OrderBy(_ => _rng.Next()).Take(count).ToList();
}
