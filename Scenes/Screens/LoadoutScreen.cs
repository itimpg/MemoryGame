using System.Collections.Generic;
using System.Linq;
using Godot;
using MemoryGame.Core;
using MemoryGame.Progress;

namespace MemoryGame.UI;

/// <summary>Before a run: pick up to <see cref="SkillCatalog.MaxEquipped"/> unlocked skills to take along.</summary>
public partial class LoadoutScreen : VBoxContainer
{
	[Signal]
	public delegate void StartRequestedEventHandler();

	[Signal]
	public delegate void BackRequestedEventHandler();

	private readonly Dictionary<Button, Skill> _skillButtons = [];
	private VBoxContainer _skillList = null!;
	private Label _selectionLabel = null!;

	public override void _Ready()
	{
		_skillList = GetNode<VBoxContainer>("%SkillList");
		_selectionLabel = GetNode<Label>("%SelectionLabel");
		GetNode<Button>("%StartButton").Pressed += () => EmitSignal(SignalName.StartRequested);
		GetNode<Button>("%BackButton").Pressed += () => EmitSignal(SignalName.BackRequested);
	}

	/// <summary>The skills currently selected, in catalog order.</summary>
	public IReadOnlyList<Skill> SelectedSkills =>
		_skillButtons.Where(pair => pair.Key.ButtonPressed).Select(pair => pair.Value)
			.OrderBy(s => s.UnlockAtStage).ToList();

	public void Display(PlayerProfile profile)
	{
		foreach (var child in _skillList.GetChildren())
			child.QueueFree();
		_skillButtons.Clear();

		var equipped = profile.EquippedSkills;
		foreach (var skill in SkillCatalog.All)
		{
			bool unlocked = profile.IsUnlocked(skill);
			var button = new Button
			{
				Text = unlocked
					? $"{skill.Name}\n{skill.Description}"
					: $"Locked\nClear stage {skill.UnlockAtStage} in a run to unlock.",
				Icon = unlocked ? ArtLoader.SkillIcon(skill.Id) : null,
				ThemeTypeVariation = "UpgradeButton",
				Alignment = HorizontalAlignment.Left,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(0, 110),
				ToggleMode = true,
				ButtonPressed = equipped.Contains(skill),
				Disabled = !unlocked,
			};
			button.Toggled += pressed => OnSkillToggled(button, pressed);
			_skillList.AddChild(button);
			_skillButtons[button] = skill;
		}
		UpdateSelectionLabel();
	}

	private void OnSkillToggled(Button button, bool pressed)
	{
		// Refuse a selection beyond the limit rather than silently dropping another one.
		if (pressed && _skillButtons.Keys.Count(b => b.ButtonPressed) > SkillCatalog.MaxEquipped)
			button.SetPressedNoSignal(false);
		UpdateSelectionLabel();
	}

	private void UpdateSelectionLabel()
	{
		int selected = _skillButtons.Keys.Count(b => b.ButtonPressed);
		_selectionLabel.Text = $"Equipped {selected} / {SkillCatalog.MaxEquipped} — each can be used once per battle";
	}
}
