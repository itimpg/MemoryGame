using System.Linq;
using MemoryGame.HighScores;
using Xunit;

namespace MemoryGame.Tests;

public class HighScoreTableTests
{
	private static HighScoreEntry Entry(int score, string date = "2026-01-01 00:00") => new(score, 1, 1, date);

	[Fact]
	public void Keeps_entries_sorted_and_returns_rank()
	{
		var table = new HighScoreTable(10);
		Assert.Equal(1, table.Add(Entry(100)));
		Assert.Equal(1, table.Add(Entry(300)));
		Assert.Equal(2, table.Add(Entry(200)));
		Assert.Equal([300, 200, 100], table.Entries.Select(e => e.Score));
	}

	[Fact]
	public void Earlier_score_stays_ahead_on_a_tie()
	{
		var table = new HighScoreTable(10);
		table.Add(Entry(100, "first"));
		Assert.Equal(2, table.Add(Entry(100, "second")));
		Assert.Equal("first", table.Entries[0].Date);
	}

	[Fact]
	public void Drops_the_lowest_entry_when_full()
	{
		var table = new HighScoreTable(3);
		foreach (int score in new[] { 100, 200, 300 })
			table.Add(Entry(score));

		Assert.Null(table.Add(Entry(50)));
		Assert.Null(table.Add(Entry(100))); // A tie with the last place doesn't push it out.
		Assert.Equal(3, table.Add(Entry(150)));
		Assert.Equal([300, 200, 150], table.Entries.Select(e => e.Score));
	}

	[Fact]
	public void Ignores_zero_scores()
	{
		var table = new HighScoreTable(10);
		Assert.Null(table.Add(Entry(0)));
		Assert.Empty(table.Entries);
	}

	[Fact]
	public void Sorts_and_trims_loaded_entries()
	{
		var table = new HighScoreTable(2, [Entry(100), Entry(300), Entry(200)]);
		Assert.Equal([300, 200], table.Entries.Select(e => e.Score));
	}
}
