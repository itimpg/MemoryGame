using Godot;
using MemoryGame.Audio;
using MemoryGame.Core;

namespace MemoryGame.UI;

/// <summary>Full-screen "Congratulations, you unlocked a skill" popup, shown over the game over screen.</summary>
public partial class SkillUnlockPopup : Control
{
	private TextureRect _icon = null!;
	private Label _skillLabel = null!;
	private Label _descriptionLabel = null!;
	private Control _panel = null!;

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("%SkillIcon");
		_skillLabel = GetNode<Label>("%SkillLabel");
		_descriptionLabel = GetNode<Label>("%DescriptionLabel");
		_panel = GetNode<Control>("%Panel");
		GetNode<Button>("%OkButton").Pressed += Hide;
	}

	public void Present(Skill skill)
	{
		_icon.Texture = ArtLoader.SkillIcon(skill.Id);
		_skillLabel.Text = $"You unlocked a new skill:\n{skill.Name}";
		_descriptionLabel.Text =
			$"{skill.Description}\n\n" +
			"Equip it on the Skills screen before your next run. Each skill can be used once per battle.";
		Show();

		// Pop in once the panel has its size, so it scales from the center.
		_panel.Scale = new Vector2(0.6f, 0.6f);
		Callable.From(() =>
		{
			_panel.PivotOffset = _panel.Size / 2;
			_panel.CreateTween()
				.TweenProperty(_panel, "scale", Vector2.One, 0.35)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		}).CallDeferred();

		AudioManager.Instance.Play(Sfx.SkillUnlock);
	}
}
