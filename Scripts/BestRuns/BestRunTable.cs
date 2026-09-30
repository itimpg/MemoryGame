using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;

namespace MemoryGame.BestRuns;

/// <param name="Stage">The stage the run reached (the one the player lost or quit on).</param>
public sealed record BestRunEntry(int Stage, int Damage, int Correct, int Attempts, string Date)
{
	public static BestRunEntry FromRun(Run run, DateTime playedAt) =>
		new(run.Stage, run.Stats.TotalDamage, run.Stats.Correct, run.Stats.Attempts, playedAt.ToString("yyyy-MM-dd HH:mm"));

	/// <summary>Furthest stage first, then most damage.</summary>
	public bool IsBetterThan(BestRunEntry other) =>
		Stage != other.Stage ? Stage > other.Stage : Damage > other.Damage;
}

/// <summary>The best runs, best first. On a tie the earlier run stays ahead.</summary>
public sealed class BestRunTable
{
	private readonly List<BestRunEntry> _entries;

	public BestRunTable(int capacity, IEnumerable<BestRunEntry>? entries = null)
	{
		Capacity = capacity;
		_entries = (entries ?? [])
			.OrderByDescending(e => e.Stage)
			.ThenByDescending(e => e.Damage)
			.Take(capacity)
			.ToList();
	}

	public int Capacity { get; }
	public IReadOnlyList<BestRunEntry> Entries => _entries;

	/// <summary>Adds the entry if it makes the table. Returns its 1-based rank, or null if it didn't.</summary>
	public int? Add(BestRunEntry entry)
	{
		if (entry.Damage <= 0)
			return null;

		int index = _entries.FindIndex(entry.IsBetterThan);
		if (index < 0)
			index = _entries.Count;
		if (index >= Capacity)
			return null;

		_entries.Insert(index, entry);
		if (_entries.Count > Capacity)
			_entries.RemoveAt(_entries.Count - 1);
		return index + 1;
	}
}
