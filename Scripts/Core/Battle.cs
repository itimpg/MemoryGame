using System;
using System.Text;

namespace MemoryGame.Core;

public enum BattlePhase { NotStarted, Showing, Input, Feedback, Won, Lost }

/// <summary>
/// One stage: defeat the monster before the clock runs out. Independent of Godot — drive it
/// with <see cref="Tick"/>, feed it digits with <see cref="EnterDigit"/>, and subscribe to the
/// events to update the UI.
/// </summary>
public sealed class Battle
{
	private readonly GameRules _rules;
	private readonly Random _rng;
	private readonly DifficultyTracker _difficulty;
	private readonly RunStats _stats;
	private readonly UpgradeSet _upgrades;
	private int _combo;
	private int _freeMistakesLeft;

	public Battle(GameRules rules, Random rng, StageSetup setup, RunStats stats, UpgradeSet upgrades)
	{
		_rules = rules;
		_rng = rng;
		_difficulty = new DifficultyTracker(rules, setup.StartingDigits);
		_stats = stats;
		_upgrades = upgrades;
		Monster = setup.Monster;
		ShowDuration = setup.ShowDuration + upgrades.ExtraShowTime;
	}

	public event Action<BattlePhase>? PhaseChanged;
	public event Action<AnswerResult>? AnswerChecked;

	public Monster Monster { get; }

	/// <summary>How long each number is shown in this stage.</summary>
	public float ShowDuration { get; }
	public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
	public bool IsOver => Phase is BattlePhase.Won or BattlePhase.Lost;
	public float TimeLeft { get; private set; }
	public float PhaseElapsed { get; private set; }
	public string CurrentNumber { get; private set; } = "";
	public string Entered { get; private set; } = "";

	/// <summary>Digit count from the difficulty tracker, before upgrades.</summary>
	public int Digits => _difficulty.Digits;

	/// <summary>How long the numbers actually are, including extra digits from upgrades.</summary>
	public int NumberLength => Digits + _upgrades.ExtraDigits;

	public bool IsAtMaxDigits => Digits >= _rules.MaxDigits;
	public int CorrectStreak => _difficulty.CorrectStreak;
	public int CorrectStreakNeeded => _difficulty.CorrectStreakNeeded;

	public void Start()
	{
		if (Phase != BattlePhase.NotStarted)
			throw new InvalidOperationException("A battle can only be started once.");
		TimeLeft = _rules.StageDuration + _upgrades.ExtraStageTime;
		_freeMistakesLeft = _upgrades.FreeMistakesPerStage;
		NextRound();
	}

	public void Tick(float delta)
	{
		if (Phase == BattlePhase.NotStarted || IsOver)
			return;

		TimeLeft = Math.Max(0f, TimeLeft - delta);
		PhaseElapsed += delta;

		if (TimeLeft <= 0f)
			SetPhase(BattlePhase.Lost);
		else if (Phase == BattlePhase.Showing && PhaseElapsed >= ShowDuration)
			BeginInput();
		else if (Phase == BattlePhase.Feedback && PhaseElapsed >= _rules.FeedbackDuration)
			NextRound();
	}

	/// <summary>Adds a digit to the answer. The answer is checked as soon as it has as many digits as the number.</summary>
	public void EnterDigit(int digit)
	{
		if (Phase != BattlePhase.Input || digit is < 0 or > 9)
			return;

		Entered += (char)('0' + digit);
		if (Entered.Length == CurrentNumber.Length)
			CheckAnswer();
	}

	private void BeginInput()
	{
		// The Hint upgrade fills in the first digit.
		Entered = _upgrades.RevealsFirstDigit ? CurrentNumber[..1] : "";
		SetPhase(BattlePhase.Input);
	}

	private void CheckAnswer()
	{
		float answerTime = PhaseElapsed;
		bool isCorrect = Entered == CurrentNumber;
		int damage = 0;
		float damageMultiplier = 1f;
		float timePenalty = 0f;
		bool mistakeForgiven = false;

		_stats.Attempts++;
		if (isCorrect)
		{
			_combo++;
			damageMultiplier = _upgrades.DamageMultiplier(_combo);
			damage = (int)MathF.Round(Damage.Calculate(CurrentNumber.Length, answerTime, _rules) * damageMultiplier);
			Monster.TakeDamage(damage);
			_stats.TotalDamage += damage;
			_stats.Correct++;
			_stats.TotalCorrectAnswerTime += answerTime;
		}
		else
		{
			_combo = 0;
			if (_freeMistakesLeft > 0)
			{
				_freeMistakesLeft--;
				mistakeForgiven = true;
			}
			else
			{
				timePenalty = Math.Min(_rules.WrongAnswerTimePenalty, TimeLeft);
				TimeLeft -= timePenalty;
			}
		}
		int digitChange = _difficulty.RecordAnswer(isCorrect);

		AnswerChecked?.Invoke(new AnswerResult(
			isCorrect, CurrentNumber, Entered, damage, damageMultiplier, timePenalty, mistakeForgiven,
			answerTime, digitChange, NumberLength));

		if (Monster.IsDefeated)
		{
			_stats.StagesCleared++;
			SetPhase(BattlePhase.Won);
		}
		else if (TimeLeft <= 0f)
		{
			SetPhase(BattlePhase.Lost);
		}
		else
		{
			SetPhase(BattlePhase.Feedback);
		}
	}

	private void NextRound()
	{
		CurrentNumber = RandomNumber(NumberLength);
		Entered = "";
		SetPhase(BattlePhase.Showing);
	}

	private string RandomNumber(int digits)
	{
		var number = new StringBuilder(digits);
		number.Append(_rng.Next(1, 10)); // No leading zero.
		for (int i = 1; i < digits; i++)
			number.Append(_rng.Next(0, 10));
		return number.ToString();
	}

	private void SetPhase(BattlePhase phase)
	{
		Phase = phase;
		PhaseElapsed = 0f;
		PhaseChanged?.Invoke(phase);
	}
}
