using System.Collections.Generic;
using Godot;

namespace MemoryGame.UI;

/// <summary>On-screen number pad. Every child Button whose text is a digit becomes a key.</summary>
public partial class Keypad : GridContainer
{
	[Signal]
	public delegate void DigitPressedEventHandler(int digit);

	private readonly List<Button> _keys = [];

	public override void _Ready()
	{
		foreach (var child in GetChildren())
		{
			if (child is not Button key || !int.TryParse(key.Text, out int digit))
				continue;
			key.Pressed += () => EmitSignal(SignalName.DigitPressed, digit);
			_keys.Add(key);
		}
	}

	public void SetEnabled(bool enabled)
	{
		foreach (var key in _keys)
			key.Disabled = !enabled;
	}
}
