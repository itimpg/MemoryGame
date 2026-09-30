using System.Linq;
using MemoryGame.BestRuns;
using Xunit;

namespace MemoryGame.Tests;

public class BestRunTableTests
{
	private static BestRunEntry Entry(int stage, int damage, string date = "2026-01-01 00:00") =>
		new(stage, damage, 1, 1, date);

	[Fact]
	public void Ranks_by_stage_then_damage()
	{
		var table = new BestRunTable(10);
		Assert.Equal(1, table.Add(Entry(2, 500)));
		Assert.Equal(1, table.Add(Entry(3, 100))); // Further stage beats more damage.
		Assert.Equal(2, table.Add(Entry(2, 900)));
		Assert.Equal(
			[(3, 100), (2, 900), (2, 500)],
			table.Entries.Select(e => (e.Stage, e.Damage)));
	}

	[Fact]
	public void Earlier_run_stays_ahead_on_a_tie()
	{
		var table = new BestRunTable(10);
		table.Add(Entry(2, 100, "first"));
		Assert.Equal(2, table.Add(Entry(2, 100, "second")));
		Assert.Equal("first", table.Entries[0].Date);
	}

	[Fact]
	public void Drops_the_worst_entry_when_full()
	{
		var table = new BestRunTable(3);
		foreach (int stage in new[] { 1, 2, 3 })
			table.Add(Entry(stage, 100));

		Assert.Null(table.Add(Entry(1, 50)));
		Assert.Null(table.Add(Entry(1, 100))); // A tie with the last place doesn't push it out.
		Assert.Equal(3, table.Add(Entry(1, 150)));
		Assert.Equal([3, 2, 1], table.Entries.Select(e => e.Stage));
		Assert.Equal(150, table.Entries[2].Damage);
	}

	[Fact]
	public void Ignores_runs_without_damage()
	{
		var table = new BestRunTable(10);
		Assert.Null(table.Add(Entry(1, 0)));
		Assert.Empty(table.Entries);
	}

	[Fact]
	public void Sorts_and_trims_loaded_entries()
	{
		var table = new BestRunTable(2, [Entry(1, 100), Entry(3, 50), Entry(3, 80)]);
		Assert.Equal([(3, 80), (3, 50)], table.Entries.Select(e => (e.Stage, e.Damage)));
	}
}
