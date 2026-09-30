using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MemoryGame.Core;
using Xunit;

namespace MemoryGame.Tests;

/// <summary>Every id the game looks up by naming convention (see ArtLoader) has a file in Art/.</summary>
public class ArtTests
{
	private static readonly string[] Extensions = [".png", ".webp", ".svg"];

	private static string ArtFolder(string subfolder, [CallerFilePath] string testFile = "") =>
		Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "../../Art", subfolder));

	private static IEnumerable<string> Missing(string subfolder, IEnumerable<string> ids) =>
		ids.Where(id => !Extensions.Any(ext => File.Exists(Path.Combine(ArtFolder(subfolder), id + ext))));

	[Fact]
	public void Every_monster_has_art()
	{
		// Names cycle, so 20 stages covers every monster with room to spare.
		var ids = Enumerable.Range(1, 20).Select(stage => Monster.ForStage(stage, GameRules.Default).Id).Distinct();
		Assert.Empty(Missing("Monsters", ids));
	}

	[Fact]
	public void Every_upgrade_has_an_icon() =>
		Assert.Empty(Missing("Upgrades", UpgradeCatalog.All.Select(u => u.Id)));

	[Theory]
	[InlineData("Monsters", "monster")]
	[InlineData("Upgrades", "upgrade")]
	public void Fallback_art_exists(string subfolder, string fallbackId) =>
		Assert.True(File.Exists(Path.Combine(ArtFolder(subfolder), fallbackId + ".svg")));
}
