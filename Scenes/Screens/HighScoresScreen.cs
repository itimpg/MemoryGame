using System.Collections.Generic;
using Godot;
using MemoryGame.HighScores;

namespace MemoryGame.UI;

public partial class HighScoresScreen : VBoxContainer
{
	[Signal]
	public delegate void BackRequestedEventHandler();

	private GridContainer _table = null!;
	private Label _emptyLabel = null!;

	public override void _Ready()
	{
		_table = GetNode<GridContainer>("%Table");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		GetNode<Button>("%BackButton").Pressed += () => EmitSignal(SignalName.BackRequested);
	}

	public void Display(IReadOnlyList<HighScoreEntry> entries)
	{
		foreach (var child in _table.GetChildren())
			child.QueueFree();

		_emptyLabel.Visible = entries.Count == 0;
		_table.Visible = entries.Count > 0;
		if (entries.Count == 0)
			return;

		foreach (var header in new[] { "#", "Score", "Correct", "Date" })
			AddCell(header, "TableHeaderLabel");
		for (int i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];
			AddCell($"{i + 1}");
			AddCell($"{entry.Score}");
			AddCell($"{entry.Correct}/{entry.Attempts}");
			AddCell(entry.Date);
		}
	}

	private void AddCell(string text, string themeType = "TableLabel")
	{
		_table.AddChild(new Label
		{
			Text = text,
			ThemeTypeVariation = themeType,
			HorizontalAlignment = HorizontalAlignment.Center,
		});
	}
}
