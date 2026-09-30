using System;
using Godot;
using MemoryGame.Core;

namespace MemoryGame.UI;

public partial class GameScreen : VBoxContainer
{
	[Signal]
	public delegate void GameFinishedEventHandler(int score, int correct, int attempts, float totalCorrectAnswerTime);

	[Signal]
	public delegate void PauseRequestedEventHandler();

	private static readonly Color CorrectColor = new(0.4f, 1f, 0.5f);
	private static readonly Color WrongColor = new(1f, 0.45f, 0.45f);
	private static readonly Color PlaceholderColor = new(1f, 1f, 1f, 0.35f);

	private readonly GameRules _rules = GameRules.Default;
	private readonly Random _rng = new();
	private GameSession? _session;

	private Label _timeLabel = null!;
	private Label _scoreLabel = null!;
	private Label _numberLabel = null!;
	private Label _messageLabel = null!;
	private ProgressBar _showBar = null!;
	private Keypad _keypad = null!;

	public override void _Ready()
	{
		_timeLabel = GetNode<Label>("%TimeLabel");
		_scoreLabel = GetNode<Label>("%ScoreLabel");
		_numberLabel = GetNode<Label>("%NumberLabel");
		_messageLabel = GetNode<Label>("%MessageLabel");
		_showBar = GetNode<ProgressBar>("%ShowBar");
		_keypad = GetNode<Keypad>("%Keypad");

		_showBar.MaxValue = _rules.ShowDuration;
		_keypad.DigitPressed += OnDigitPressed;
		GetNode<Button>("%PauseButton").Pressed += RequestPause;
		SetProcess(false);
	}

	public bool IsPlaying => _session is not null;

	public void StartGame()
	{
		_session = new GameSession(_rules, _rng);
		_session.PhaseChanged += OnPhaseChanged;
		_session.AnswerChecked += OnAnswerChecked;
		_session.Start();
		UpdateHud();
		SetProcess(true);
	}

	/// <summary>Ends the current game without reporting a result.</summary>
	public void Abandon()
	{
		SetProcess(false);
		_session = null;
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
		if (_session is null)
			return;

		_session.Tick((float)delta);
		if (_session is null) // The game just finished.
			return;
		UpdateHud();
		if (_session.Phase == RoundPhase.Showing)
			_showBar.Value = _rules.ShowDuration - _session.PhaseElapsed;
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
		if (_session?.Phase != RoundPhase.Input || @event is not InputEventKey { Pressed: true, Echo: false } key)
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
		if (_session is null)
			return;
		_session.EnterDigit(digit);
		// Once the last digit is in, OnAnswerChecked has already shown the result.
		if (_session.Phase == RoundPhase.Input)
			ShowEntered();
	}

	private void OnPhaseChanged(RoundPhase phase)
	{
		switch (phase)
		{
			case RoundPhase.Showing:
				SetText(_numberLabel, _session!.CurrentNumber);
				SetText(_messageLabel, "Remember this!");
				_showBar.Value = _rules.ShowDuration;
				_showBar.Show();
				_keypad.SetEnabled(false);
				break;
			case RoundPhase.Input:
				SetText(_messageLabel, "Enter the number");
				_showBar.Hide();
				ShowEntered();
				_keypad.SetEnabled(true);
				break;
			case RoundPhase.Finished:
				SetProcess(false);
				var result = _session!.Result;
				_session = null;
				EmitSignal(SignalName.GameFinished, result.Score, result.Correct, result.Attempts, result.TotalCorrectAnswerTime);
				break;
		}
	}

	private void OnAnswerChecked(AnswerResult answer)
	{
		_keypad.SetEnabled(false);
		SetText(_numberLabel, answer.Expected);

		string levelText = answer.DigitChange switch
		{
			> 0 => $"\nLevel up! {answer.Digits} digits",
			< 0 => $"\nLevel down: {answer.Digits} digits",
			_ => "",
		};
		if (answer.IsCorrect)
			SetText(_messageLabel, $"Correct! +{answer.Points}  ({answer.AnswerTime:0.00}s){levelText}", CorrectColor);
		else
			SetText(_messageLabel, $"Wrong — you entered {answer.Entered}{levelText}", WrongColor);
	}

	/// <summary>Shows what the player has entered so far, with underscores for the remaining digits.</summary>
	private void ShowEntered()
	{
		var session = _session!;
		string text = session.Entered.PadRight(session.CurrentNumber.Length, '_');
		SetText(_numberLabel, text, session.Entered.Length == 0 ? PlaceholderColor : null);
	}

	private void UpdateHud()
	{
		if (_session is null)
			return;
		_timeLabel.Text = $"Time: {Mathf.CeilToInt(_session.TimeLeft)}s";
		_scoreLabel.Text = $"Score: {_session.Score}";
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
