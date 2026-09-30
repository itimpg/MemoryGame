using System.Collections.Generic;
using Godot;
using MemoryGame.BestRuns;

namespace MemoryGame.UI;

public partial class BestRunsScreen : VBoxContainer
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

	public void Display(IReadOnlyList<BestRunEntry> entries)
	{
		foreach (var child in _table.GetChildren())
			child.QueueFree();

		_emptyLabel.Visible = entries.Count == 0;
		_table.Visible = entries.Count > 0;
		if (entries.Count == 0)
			return;

		foreach (var header in new[] { "#", "Stage", "Damage", "Date" })
			AddCell(header, "TableHeaderLabel");
		for (int i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];
			AddCell($"{i + 1}");
			AddCell($"{entry.Stage}");
			AddCell($"{entry.Damage}");
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
