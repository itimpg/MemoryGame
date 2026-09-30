using Godot;
using MemoryGame.Audio;
using MemoryGame.Core;

namespace MemoryGame.UI;

/// <summary>Shows one <see cref="Battle"/>: the monster, its health, the number to remember and the keypad.</summary>
public partial class GameScreen : VBoxContainer
{
	[Signal]
	public delegate void BattleEndedEventHandler(bool won);

	[Signal]
	public delegate void PauseRequestedEventHandler();

	private static readonly Color CorrectColor = new(0.4f, 1f, 0.5f);
	private static readonly Color WrongColor = new(1f, 0.45f, 0.45f);
	private static readonly Color PlaceholderColor = new(1f, 1f, 1f, 0.35f);
	private static readonly Color HitFlashColor = new(1f, 0.3f, 0.3f);

	private Battle? _battle;
	private Tween? _spriteTween;
	private Tween? _flashTween;
	private Tween? _defeatTween;

	private const int TimerWarningSeconds = 5;
	private const int CountdownSeconds = 3;
	private const int DefeatBlinks = 5;
	private int _lastTickSecond;
	private Tween? _healthTween;

	private Label _stageLabel = null!;
	private Label _timeLabel = null!;
	private Label _streakLabel = null!;
	private Label _healthLabel = null!;
	private ProgressBar _healthBar = null!;
	private Control _monsterStage = null!;
	private TextureRect _monsterSprite = null!;
	private Label _numberLabel = null!;
	private Label _messageLabel = null!;
	private ProgressBar _showBar = null!;
	private Keypad _keypad = null!;

	public override void _Ready()
	{
		_stageLabel = GetNode<Label>("%StageLabel");
		_timeLabel = GetNode<Label>("%TimeLabel");
		_streakLabel = GetNode<Label>("%StreakLabel");
		_healthLabel = GetNode<Label>("%HealthLabel");
		_healthBar = GetNode<ProgressBar>("%HealthBar");
		_monsterStage = GetNode<Control>("%MonsterStage");
		_monsterSprite = GetNode<TextureRect>("%MonsterSprite");
		_numberLabel = GetNode<Label>("%NumberLabel");
		_messageLabel = GetNode<Label>("%MessageLabel");
		_showBar = GetNode<ProgressBar>("%ShowBar");
		_keypad = GetNode<Keypad>("%Keypad");

		_keypad.DigitPressed += OnDigitPressed;
		GetNode<Button>("%PauseButton").Pressed += RequestPause;
		SetProcess(false);
	}

	public bool IsPlaying => _battle is not null;

	public void StartBattle(Battle battle, int stage)
	{
		_battle = battle;
		_battle.PhaseChanged += OnPhaseChanged;
		_battle.AnswerChecked += OnAnswerChecked;

		_stageLabel.Text = $"Stage {stage}";
		_healthTween?.Kill();
		_healthBar.MaxValue = battle.Monster.MaxHealth;
		_healthBar.Value = battle.Monster.Health;
		UpdateHealthLabel();
		_monsterSprite.Texture = ArtLoader.MonsterTexture(battle.Monster.Id);
		ResetMonsterSprite();
		_showBar.MaxValue = battle.ShowDuration;
		UpdateStreakLabel(battle);
		_lastTickSecond = int.MaxValue;
		UpdateHud();

		RunCountdown(battle);
	}

	/// <summary>"3, 2, 1, Fight!" before the clock starts. The battle only starts (and the time only runs) afterwards.</summary>
	private async void RunCountdown(Battle battle)
	{
		_keypad.SetEnabled(false);
		SetShowBarVisible(false);
		SetText(_messageLabel, "Get ready!");

		for (int n = CountdownSeconds; n >= 1; n--)
		{
			ShowCountdownStep(n.ToString(), Sfx.CountdownTick);
			await ToSignal(GetTree().CreateTimer(1.0, processAlways: false), Timer.SignalName.Timeout);
			if (_battle != battle) // Abandoned from the pause menu meanwhile.
				return;
		}

		ShowCountdownStep("Fight!", Sfx.CountdownGo);
		await ToSignal(GetTree().CreateTimer(0.5, processAlways: false), Timer.SignalName.Timeout);
		if (_battle != battle)
			return;

		battle.Start();
		SetProcess(true);
	}

	private void ShowCountdownStep(string text, Sfx sound)
	{
		SetText(_numberLabel, text);
		_numberLabel.PivotOffset = _numberLabel.Size / 2;
		_numberLabel.Scale = new Vector2(1.6f, 1.6f);
		_numberLabel.CreateTween()
			.TweenProperty(_numberLabel, "scale", Vector2.One, 0.3)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		AudioManager.Instance.Play(sound);
	}

	/// <summary>Ends the current battle without reporting a result.</summary>
	public void Abandon()
	{
		SetProcess(false);
		_battle = null;
	}

	public override void _Notification(int what)
	{
		// Don't let the clock run while the player is in another app.
		if (what is (int)NotificationApplicationFocusOut or (int)NotificationApplicationPaused)
			RequestPause();
	}

	private void RequestPause()
	{
		if (IsPlaying)
			EmitSignal(SignalName.PauseRequested);
	}

	public override void _Process(double delta)
	{
		if (_battle is null)
			return;

		_battle.Tick((float)delta);
		UpdateHud();
		if (_battle.Phase == BattlePhase.Showing)
			_showBar.Value = _battle.ShowDuration - _battle.PhaseElapsed;
		PlayTimerWarning(_battle);
	}

	/// <summary>Ticks once per second during the last seconds of a stage.</summary>
	private void PlayTimerWarning(Battle battle)
	{
		int secondsLeft = Mathf.CeilToInt(battle.TimeLeft);
		if (battle.IsOver || secondsLeft > TimerWarningSeconds || secondsLeft == _lastTickSecond)
			return;
		_lastTickSecond = secondsLeft;
		AudioManager.Instance.Play(Sfx.TimerTick);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsPlaying && @event.IsActionPressed("ui_cancel"))
		{
			RequestPause();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Physical keyboard support for desktop testing.
		if (_battle?.Phase != BattlePhase.Input || @event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		if (key.Keycode is >= Key.Key0 and <= Key.Key9)
			OnDigitPressed((int)(key.Keycode - Key.Key0));
		else if (key.Keycode is >= Key.Kp0 and <= Key.Kp9)
			OnDigitPressed((int)(key.Keycode - Key.Kp0));
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	private void OnDigitPressed(int digit)
	{
		if (_battle is null)
			return;
		if (_battle.Phase == BattlePhase.Input)
			AudioManager.Instance.Play(Sfx.KeyPress);
		_battle.EnterDigit(digit);
		// Once the last digit is in, OnAnswerChecked has already shown the result.
		if (_battle.Phase == BattlePhase.Input)
			ShowEntered(_battle);
	}

	private void OnPhaseChanged(BattlePhase phase)
	{
		var battle = _battle!;
		switch (phase)
		{
			case BattlePhase.Showing:
				SetText(_numberLabel, battle.CurrentNumber);
				SetText(_messageLabel, "Remember this!");
				AudioManager.Instance.Play(Sfx.NumberShow);
				_showBar.Value = battle.ShowDuration;
				SetShowBarVisible(true);
				_keypad.SetEnabled(false);
				break;
			case BattlePhase.Input:
				SetText(_messageLabel, "Enter the number");
				SetShowBarVisible(false);
				ShowEntered(battle);
				_keypad.SetEnabled(true);
				break;
			case BattlePhase.Won:
			case BattlePhase.Lost:
				FinishBattle(battle, won: phase == BattlePhase.Won);
				break;
		}
	}

	private async void FinishBattle(Battle battle, bool won)
	{
		SetProcess(false);
		_keypad.SetEnabled(false);
		SetShowBarVisible(false);

		if (won)
		{
			SetText(_messageLabel, $"{battle.Monster.Name} defeated!", CorrectColor);
			AudioManager.Instance.Play(Sfx.MonsterDefeated);
			await ToSignal(PlayDefeatAnimation(), Tween.SignalName.Finished);
		}
		else
		{
			SetText(_messageLabel, "Time's up!", WrongColor);
			AudioManager.Instance.Play(Sfx.GameOver);
			await ToSignal(GetTree().CreateTimer(0.8, processAlways: false), Timer.SignalName.Timeout);
		}

		if (_battle != battle) // Abandoned from the pause menu meanwhile.
			return;
		_battle = null;
		EmitSignal(SignalName.BattleEnded, won);
	}

	private void OnAnswerChecked(AnswerResult answer)
	{
		_keypad.SetEnabled(false);
		SetText(_numberLabel, answer.Expected);

		string levelText = answer.DigitChange switch
		{
			> 0 => $"\nLevel up! {answer.NumberLength} digits",
			< 0 => $"\nLevel down: {answer.NumberLength} digits",
			_ => "",
		};

		if (answer.IsCorrect)
		{
			string bonus = answer.DamageMultiplier > 1.001f ? $" (x{answer.DamageMultiplier:0.##})" : "";
			SetText(_messageLabel, $"Hit! {answer.Damage} damage{bonus}  {answer.AnswerTime:0.00}s{levelText}", CorrectColor);
			PlayHit(answer.Damage);
			AudioManager.Instance.Play(Sfx.Hit);
		}
		else if (answer.MistakeForgiven)
		{
			SetText(_messageLabel, $"Wrong — you entered {answer.Entered}\nSecond Chance: no time lost{levelText}", WrongColor);
			ShowPopup("Blocked!", CorrectColor);
			AudioManager.Instance.Play(Sfx.Blocked);
		}
		else
		{
			string monster = _battle!.Monster.Name;
			SetText(_messageLabel, $"Wrong — you entered {answer.Entered}\nThe {monster} strikes! -{answer.TimePenalty:0}s{levelText}", WrongColor);
			PlayMonsterAttack(answer.TimePenalty);
			AudioManager.Instance.Play(Sfx.Miss);
		}

		if (answer.DigitChange > 0)
			AudioManager.Instance.Play(Sfx.LevelUp);
		else if (answer.DigitChange < 0)
			AudioManager.Instance.Play(Sfx.LevelDown);

		UpdateStreakLabel(_battle!);
		if (answer.IsCorrect)
			PulseStreakLabel();
	}

	/// <summary>Shows progress toward the next digit, e.g. "Streak 3/4 (next: 5 digits)".</summary>
	private void UpdateStreakLabel(Battle battle)
	{
		_streakLabel.Text = battle.IsAtMaxDigits
			? $"Max {battle.NumberLength} digits"
			: $"Streak {battle.CorrectStreak}/{battle.CorrectStreakNeeded} (next: {battle.NumberLength + 1} digits)";
	}

	private void PulseStreakLabel()
	{
		_streakLabel.PivotOffset = _streakLabel.Size / 2;
		var tween = _streakLabel.CreateTween();
		tween.TweenProperty(_streakLabel, "scale", new Vector2(1.25f, 1.25f), 0.08);
		tween.TweenProperty(_streakLabel, "scale", Vector2.One, 0.15);
	}

	// Fades instead of hiding so the layout (and the monster's size) doesn't jump between phases.
	private void SetShowBarVisible(bool visible) =>
		_showBar.Modulate = visible ? Colors.White : Colors.Transparent;

	private void PlayHit(int damage)
	{
		ShowPopup($"{damage}");

		ResetMonsterSprite();
		_monsterSprite.Modulate = HitFlashColor;
		_flashTween = CreateTween();
		_flashTween.TweenProperty(_monsterSprite, "modulate", Colors.White, 0.25);
		_spriteTween = CreateTween();
		foreach (float x in new[] { 14f, -12f, 8f, -4f, 0f })
			_spriteTween.TweenProperty(_monsterSprite, "position:x", x, 0.04);

		var monster = _battle!.Monster;
		UpdateHealthLabel();
		_healthTween?.Kill();
		_healthTween = CreateTween();
		_healthTween.TweenProperty(_healthBar, "value", (double)monster.Health, 0.3)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	private void PlayMonsterAttack(float timePenalty)
	{
		ShowPopup($"-{timePenalty:0}s", WrongColor);

		ResetMonsterSprite();
		_monsterSprite.PivotOffset = _monsterSprite.Size / 2;
		_spriteTween = CreateTween();
		_spriteTween.TweenProperty(_monsterSprite, "scale", new Vector2(1.2f, 1.2f), 0.1);
		_spriteTween.TweenProperty(_monsterSprite, "scale", Vector2.One, 0.15);

		_timeLabel.AddThemeColorOverride("font_color", WrongColor);
		GetTree().CreateTimer(0.5, processAlways: false).Timeout += () => _timeLabel.RemoveThemeColorOverride("font_color");
	}

	private Tween PlayDefeatAnimation()
	{
		// The battle is already over, so the clock is stopped while this plays.
		_defeatTween = CreateTween();
		_defeatTween.TweenInterval(0.3); // Let the killing blow's flash and shake (0.25s) finish first.
		for (int i = 0; i < DefeatBlinks; i++)
		{
			_defeatTween.TweenProperty(_monsterSprite, "modulate:a", 0.15f, 0.07);
			_defeatTween.TweenProperty(_monsterSprite, "modulate:a", 1f, 0.07);
		}
		_defeatTween.TweenProperty(_monsterSprite, "modulate:a", 0f, 1.0).SetEase(Tween.EaseType.Out);
		return _defeatTween;
	}

	/// <summary>Stops any running sprite effect and puts the monster back to normal.</summary>
	private void ResetMonsterSprite()
	{
		_spriteTween?.Kill();
		_flashTween?.Kill();
		_defeatTween?.Kill();
		_monsterSprite.Modulate = Colors.White;
		_monsterSprite.Scale = Vector2.One;
		_monsterSprite.Position = Vector2.Zero;
	}

	/// <summary>A number that floats up from the monster and fades out.</summary>
	private void ShowPopup(string text, Color? color = null)
	{
		var label = new Label { Text = text, ThemeTypeVariation = "DamageLabel" };
		if (color is { } c)
			label.AddThemeColorOverride("font_color", c);
		_monsterStage.AddChild(label);

		var start = new Vector2((_monsterStage.Size.X - label.GetMinimumSize().X) / 2, _monsterStage.Size.Y * 0.3f);
		label.Position = start;

		var tween = label.CreateTween().SetParallel();
		tween.TweenProperty(label, "position:y", start.Y - 90f, 0.8).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(label, "modulate:a", 0f, 0.5).SetDelay(0.3);
		tween.Chain().TweenCallback(Callable.From(label.QueueFree));
	}

	/// <summary>Shows what the player has entered so far, with underscores for the remaining digits.</summary>
	private void ShowEntered(Battle battle)
	{
		string text = battle.Entered.PadRight(battle.CurrentNumber.Length, '_');
		SetText(_numberLabel, text, battle.Entered.Length == 0 ? PlaceholderColor : null);
	}

	private void UpdateHud()
	{
		if (_battle is not null)
			_timeLabel.Text = $"{Mathf.CeilToInt(_battle.TimeLeft)}s";
	}

	private void UpdateHealthLabel()
	{
		var monster = _battle!.Monster;
		_healthLabel.Text = $"{monster.Name}   {monster.Health} / {monster.MaxHealth} HP";
	}

	private static void SetText(Label label, string text, Color? color = null)
	{
		label.Text = text;
		if (color is { } c)
			label.AddThemeColorOverride("font_color", c);
		else
			label.RemoveThemeColorOverride("font_color");
	}
}
