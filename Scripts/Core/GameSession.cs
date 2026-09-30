using System;
using System.Text;

namespace MemoryGame.Core;

public enum RoundPhase { NotStarted, Showing, Input, Feedback, Finished }

/// <summary>
/// One 60-second game, independent of Godot. Drive it with <see cref="Tick"/> and feed it
/// digits with <see cref="EnterDigit"/>; subscribe to the events to update the UI.
/// </summary>
public sealed class GameSession
{
	private readonly GameRules _rules;
	private readonly Random _rng;
	private readonly DifficultyTracker _difficulty;
	private float _totalCorrectAnswerTime;

	public GameSession(GameRules rules, Random rng)
	{
		_rules = rules;
		_rng = rng;
		_difficulty = new DifficultyTracker(rules);
	}

	public event Action<RoundPhase>? PhaseChanged;
	public event Action<AnswerResult>? AnswerChecked;

	public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;
	public float TimeLeft { get; private set; }
	public float PhaseElapsed { get; private set; }
	public int Score { get; private set; }
	public int Correct { get; private set; }
	public int Attempts { get; private set; }
	public string CurrentNumber { get; private set; } = "";
	public string Entered { get; private set; } = "";
	public int Digits => _difficulty.Digits;

	public GameResult Result => new(Score, Correct, Attempts, _totalCorrectAnswerTime);

	public void Start()
	{
		if (Phase != RoundPhase.NotStarted)
			throw new InvalidOperationException("A session can only be started once.");
		TimeLeft = _rules.GameDuration;
		NextRound();
	}

	public void Tick(float delta)
	{
		if (Phase is RoundPhase.NotStarted or RoundPhase.Finished)
			return;

		TimeLeft = Math.Max(0f, TimeLeft - delta);
		PhaseElapsed += delta;

		if (TimeLeft <= 0f)
			SetPhase(RoundPhase.Finished);
		else if (Phase == RoundPhase.Showing && PhaseElapsed >= _rules.ShowDuration)
			SetPhase(RoundPhase.Input);
		else if (Phase == RoundPhase.Feedback && PhaseElapsed >= _rules.FeedbackDuration)
			NextRound();
	}

	/// <summary>Adds a digit to the answer. The answer is checked as soon as it has as many digits as the number.</summary>
	public void EnterDigit(int digit)
	{
		if (Phase != RoundPhase.Input || digit is < 0 or > 9)
			return;

		Entered += (char)('0' + digit);
		if (Entered.Length == CurrentNumber.Length)
			CheckAnswer();
	}

	private void CheckAnswer()
	{
		float answerTime = PhaseElapsed;
		bool isCorrect = Entered == CurrentNumber;
		int points = 0;

		Attempts++;
		if (isCorrect)
		{
			points = Scoring.PointsFor(CurrentNumber.Length, answerTime, _rules);
			Score += points;
			Correct++;
			_totalCorrectAnswerTime += answerTime;
		}
		int digitChange = _difficulty.RecordAnswer(isCorrect);

		SetPhase(RoundPhase.Feedback);
		AnswerChecked?.Invoke(new AnswerResult(isCorrect, CurrentNumber, Entered, points, answerTime, digitChange, Digits));
	}

	private void NextRound()
	{
		CurrentNumber = RandomNumber(Digits);
		Entered = "";
		SetPhase(RoundPhase.Showing);
	}

	private string RandomNumber(int digits)
	{
		var number = new StringBuilder(digits);
		number.Append(_rng.Next(1, 10)); // No leading zero.
		for (int i = 1; i < digits; i++)
			number.Append(_rng.Next(0, 10));
		return number.ToString();
	}

	private void SetPhase(RoundPhase phase)
	{
		Phase = phase;
		PhaseElapsed = 0f;
		PhaseChanged?.Invoke(phase);
	}
}
