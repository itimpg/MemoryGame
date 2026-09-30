using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MemoryGame.Audio;
using Xunit;

namespace MemoryGame.Tests;

/// <summary>Every sound the game plays has a file in Audio/ (see AudioManager's naming convention).</summary>
public class AudioFileTests
{
	private static readonly string[] Extensions = [".ogg", ".wav", ".mp3"];

	private static bool Exists(string subfolder, string id, [CallerFilePath] string testFile = "")
	{
		string folder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "../../Audio", subfolder));
		return Extensions.Any(ext => File.Exists(Path.Combine(folder, id + ext)));
	}

	[Fact]
	public void Every_sound_effect_has_a_file() =>
		Assert.DoesNotContain(Enum.GetValues<Sfx>().Select(SoundIds.For), id => !Exists("Sfx", id));

	[Fact]
	public void Every_music_track_has_a_file() =>
		Assert.DoesNotContain(Enum.GetValues<MusicTrack>().Select(SoundIds.For), id => !Exists("Music", id));

	[Theory]
	[InlineData(Sfx.MonsterDefeated, "monster_defeated")]
	[InlineData(Sfx.Hit, "hit")]
	public void Sound_ids_are_snake_case(Sfx sfx, string expected) =>
		Assert.Equal(expected, SoundIds.For(sfx));
}
