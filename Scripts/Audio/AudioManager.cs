using System;
using System.Collections.Generic;
using Godot;

namespace MemoryGame.Audio;

/// <summary>
/// Global sound player, registered as the <c>Audio</c> autoload. Sounds load by naming convention from
/// <c>res://Audio/Sfx/&lt;snake_case_name&gt;</c> and <c>res://Audio/Music/music_&lt;name&gt;</c> (.ogg, .wav or .mp3),
/// so final audio can replace the placeholders without code changes. A missing file just stays silent.
/// </summary>
public partial class AudioManager : Node
{
	/// <summary>Buttons in this group don't play the generic click (e.g. keypad keys, which have their own sound).</summary>
	public const string SilentButtonGroup = "silent_button";

	private const int SfxVoices = 8;
	private static readonly string[] Extensions = [".ogg", ".wav", ".mp3"];

	private readonly Dictionary<Sfx, AudioStream?> _sfx = [];
	private readonly List<AudioStreamPlayer> _sfxPlayers = [];
	private int _nextVoice;
	private AudioStreamPlayer _music = null!;
	private MusicTrack? _currentTrack;

	public static AudioManager Instance { get; private set; } = null!;

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always; // UI sounds and music keep playing while the game is paused.
	}

	public override void _Ready()
	{
		foreach (var sfx in Enum.GetValues<Sfx>())
			_sfx[sfx] = LoadStream("Sfx", SoundIds.For(sfx));

		for (int i = 0; i < SfxVoices; i++)
		{
			var player = new AudioStreamPlayer { Bus = "SFX" };
			AddChild(player);
			_sfxPlayers.Add(player);
		}

		_music = new AudioStreamPlayer { Bus = "Music" };
		AddChild(_music);
		// Loops any format, including WAV/MP3 files imported without loop points.
		_music.Finished += () => _music.Play();

		GetTree().NodeAdded += OnNodeAdded;
	}

	/// <summary>Plays a sound effect. Up to <see cref="SfxVoices"/> can overlap.</summary>
	public void Play(Sfx sfx)
	{
		if (_sfx.GetValueOrDefault(sfx) is not { } stream)
			return;
		var player = _sfxPlayers[_nextVoice];
		_nextVoice = (_nextVoice + 1) % _sfxPlayers.Count;
		player.Stream = stream;
		player.Play();
	}

	/// <summary>Switches the background music. Does nothing if that track is already playing.</summary>
	public void PlayMusic(MusicTrack track)
	{
		if (_currentTrack == track)
			return;
		_currentTrack = track;

		_music.Stream = LoadStream("Music", SoundIds.For(track));
		if (_music.Stream is null)
			_music.Stop();
		else
			_music.Play();
	}

	private void OnNodeAdded(Node node)
	{
		if (node is BaseButton button)
			button.Pressed += () =>
			{
				if (!button.IsInGroup(SilentButtonGroup))
					Play(Sfx.UiClick);
			};
	}

	private static AudioStream? LoadStream(string folder, string id)
	{
		foreach (string extension in Extensions)
		{
			string path = $"res://Audio/{folder}/{id}{extension}";
			if (ResourceLoader.Exists(path))
				return GD.Load<AudioStream>(path);
		}
		GD.PushWarning($"No audio for '{id}' in res://Audio/{folder}; it will be silent.");
		return null;
	}
}
