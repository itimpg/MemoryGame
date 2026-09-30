using System.Text.RegularExpressions;

namespace MemoryGame.Audio;

public enum Sfx
{
	UiClick,
	KeyPress,
	NumberShow,
	Hit,
	Miss,
	Blocked,
	LevelUp,
	LevelDown,
	MonsterDefeated,
	StageClear,
	UpgradePick,
	GameOver,
	TimerTick,
	CountdownTick,
	CountdownGo,
}

public enum MusicTrack { Menu, Battle }

/// <summary>File names (without extension) that each sound is loaded from.</summary>
public static class SoundIds
{
	/// <summary>Sfx.MonsterDefeated → "monster_defeated" (in Audio/Sfx).</summary>
	public static string For(Sfx sfx) => ToSnakeCase(sfx.ToString());

	/// <summary>MusicTrack.Battle → "music_battle" (in Audio/Music).</summary>
	public static string For(MusicTrack track) => "music_" + ToSnakeCase(track.ToString());

	private static string ToSnakeCase(string name) =>
		Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
