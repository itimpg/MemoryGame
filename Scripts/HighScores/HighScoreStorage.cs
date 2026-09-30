using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace MemoryGame.HighScores;

/// <summary>Loads and saves the high score table as JSON in the user data folder.</summary>
public static class HighScoreStorage
{
	private const string FilePath = "user://highscores.json";

	public static HighScoreTable Load(int capacity)
	{
		if (!FileAccess.FileExists(FilePath))
			return new HighScoreTable(capacity);

		try
		{
			using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
			if (file is null)
			{
				GD.PushWarning($"Could not open high scores: {FileAccess.GetOpenError()}");
				return new HighScoreTable(capacity);
			}
			var entries = JsonSerializer.Deserialize(file.GetAsText(), HighScoreJsonContext.Default.ListHighScoreEntry);
			return new HighScoreTable(capacity, entries);
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
			GD.PushWarning($"Could not read high scores, starting fresh: {ex.Message}");
			return new HighScoreTable(capacity);
		}
	}

	public static void Save(HighScoreTable table)
	{
		using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
		if (file is null)
		{
			GD.PushWarning($"Could not save high scores: {FileAccess.GetOpenError()}");
			return;
		}
		file.StoreString(JsonSerializer.Serialize([.. table.Entries], HighScoreJsonContext.Default.ListHighScoreEntry));
	}
}

// Source-generated serializer: reflection-based System.Text.Json doesn't work under NativeAOT (iOS exports).
[JsonSerializable(typeof(List<HighScoreEntry>))]
internal sealed partial class HighScoreJsonContext : JsonSerializerContext;
