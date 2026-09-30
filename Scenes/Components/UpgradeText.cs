using System.Linq;
using MemoryGame.Core;

namespace MemoryGame.UI;

/// <summary>Player-facing text for upgrades, shared by the screens that list them.</summary>
public static class UpgradeText
{
	public static string NameWithLevel(Upgrade upgrade, int level) =>
		upgrade.MaxLevel > 1 ? $"{upgrade.Name} Lv {level}" : upgrade.Name;

	/// <summary>e.g. "Power Strike Lv 2, Hint", or "none".</summary>
	public static string Summary(UpgradeSet upgrades)
	{
		var names = upgrades.Owned.Select(o => NameWithLevel(o.Upgrade, o.Level)).ToList();
		return names.Count > 0 ? string.Join(", ", names) : "none";
	}
}
