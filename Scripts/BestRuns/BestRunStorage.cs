using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace MemoryGame.BestRuns;

/// <summary>Loads and saves the best runs table as JSON in the user data folder.</summary>
public static class BestRunStorage
{
	// Runs are ranked by stage now, so scores from the old timed mode (highscores.json) aren't carried over.
	private const string FilePath = "user://best_runs.json";

	public static BestRunTable Load(int capacity)
	{
		if (!FileAccess.FileExists(FilePath))
			return new BestRunTable(capacity);

		try
		{
			using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
			if (file is null)
			{
				GD.PushWarning($"Could not open best runs: {FileAccess.GetOpenError()}");
				return new BestRunTable(capacity);
			}
			var entries = JsonSerializer.Deserialize(file.GetAsText(), BestRunJsonContext.Default.ListBestRunEntry);
			return new BestRunTable(capacity, entries);
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
			GD.PushWarning($"Could not read best runs, starting fresh: {ex.Message}");
			return new BestRunTable(capacity);
		}
	}

	public static void Save(BestRunTable table)
	{
		using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
		if (file is null)
		{
			GD.PushWarning($"Could not save best runs: {FileAccess.GetOpenError()}");
			return;
		}
		file.StoreString(JsonSerializer.Serialize([.. table.Entries], BestRunJsonContext.Default.ListBestRunEntry));
	}
}

// Source-generated serializer: reflection-based System.Text.Json doesn't work under NativeAOT (iOS exports).
[JsonSerializable(typeof(List<BestRunEntry>))]
internal sealed partial class BestRunJsonContext : JsonSerializerContext;
