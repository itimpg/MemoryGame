using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace MemoryGame.Progress;

/// <summary>Loads and saves the player profile as JSON in the user data folder.</summary>
public static class ProfileStorage
{
	private const string FilePath = "user://profile.json";

	// High scores and best runs from earlier versions; no longer read, only cleaned up.
	private static readonly string[] LegacyFilePaths = ["user://highscores.json", "user://best_runs.json"];

	public static PlayerProfile Load()
	{
		if (!FileAccess.FileExists(FilePath))
			return new PlayerProfile();

		try
		{
			using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
			if (file is null)
			{
				GD.PushWarning($"Could not open the profile: {FileAccess.GetOpenError()}");
				return new PlayerProfile();
			}
			return JsonSerializer.Deserialize(file.GetAsText(), ProfileJsonContext.Default.PlayerProfile) ?? new PlayerProfile();
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
			GD.PushWarning($"Could not read the profile, starting fresh: {ex.Message}");
			return new PlayerProfile();
		}
	}

	/// <summary>Deletes the saved profile, plus save files left over from earlier versions of the game.</summary>
	public static void DeleteAll()
	{
		foreach (string path in (string[])[FilePath, .. LegacyFilePaths])
		{
			if (!FileAccess.FileExists(path))
				continue;
			var error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
			if (error != Error.Ok)
				GD.PushWarning($"Could not delete {path}: {error}");
		}
	}

	public static void Save(PlayerProfile profile)
	{
		using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
		if (file is null)
		{
			GD.PushWarning($"Could not save the profile: {FileAccess.GetOpenError()}");
			return;
		}
		file.StoreString(JsonSerializer.Serialize(profile, ProfileJsonContext.Default.PlayerProfile));
	}
}

// Source-generated serializer: reflection-based System.Text.Json doesn't work under NativeAOT (iOS exports).
[JsonSerializable(typeof(PlayerProfile))]
internal sealed partial class ProfileJsonContext : JsonSerializerContext;
