using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;

namespace MemoryGame.HighScores;

public sealed record HighScoreEntry(int Score, int Correct, int Attempts, string Date)
{
	public static HighScoreEntry FromResult(GameResult result, DateTime playedAt) =>
		new(result.Score, result.Correct, result.Attempts, playedAt.ToString("yyyy-MM-dd HH:mm"));
}

/// <summary>The top scores, highest first. On a tie the earlier score stays ahead.</summary>
public sealed class HighScoreTable
{
	private readonly List<HighScoreEntry> _entries;

	public HighScoreTable(int capacity, IEnumerable<HighScoreEntry>? entries = null)
	{
		Capacity = capacity;
		_entries = (entries ?? []).OrderByDescending(e => e.Score).Take(capacity).ToList();
	}

	public int Capacity { get; }
	public IReadOnlyList<HighScoreEntry> Entries => _entries;

	/// <summary>Adds the entry if it makes the table. Returns its 1-based rank, or null if it didn't.</summary>
	public int? Add(HighScoreEntry entry)
	{
		if (entry.Score <= 0)
			return null;

		int index = _entries.FindIndex(e => e.Score < entry.Score);
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
