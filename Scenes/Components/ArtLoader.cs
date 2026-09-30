using Godot;

namespace MemoryGame.UI;

/// <summary>
/// Loads art by naming convention: <c>res://Art/&lt;folder&gt;/&lt;id&gt;.&lt;ext&gt;</c>, falling back to a
/// placeholder in the same folder. Lets final art be dropped in as PNG, WebP or SVG without code changes.
/// </summary>
public static class ArtLoader
{
	private static readonly string[] Extensions = [".png", ".webp", ".svg"];

	public static Texture2D MonsterTexture(string monsterId) => Load("Monsters", monsterId, "monster");

	public static Texture2D UpgradeIcon(string upgradeId) => Load("Upgrades", upgradeId, "upgrade");

	private static Texture2D Load(string folder, string id, string fallbackId)
	{
		foreach (string extension in Extensions)
		{
			string path = $"res://Art/{folder}/{id}{extension}";
			if (ResourceLoader.Exists(path))
				return GD.Load<Texture2D>(path);
		}
		GD.PushWarning($"No art for '{id}' in res://Art/{folder}, using the fallback.");
		return GD.Load<Texture2D>($"res://Art/{folder}/{fallbackId}.svg");
	}
}
