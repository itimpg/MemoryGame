using System;
using System.Collections.Generic;
using System.Linq;
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
	private readonly HashSet<Skill> _usedSkills = [];
	private int _combo;
	private int _freeMistakesLeft;
	private bool _replaying;
	private bool _doubleStrikeReady;

	public Battle(
		GameRules rules, Random rng, StageSetup setup, RunStats stats, UpgradeSet upgrades,
		IReadOnlyList<Skill>? skills = null)
	{
		_rules = rules;
		_rng = rng;
		_difficulty = new DifficultyTracker(rules, setup.StartingDigits);
		_stats = stats;
		_upgrades = upgrades;
		Skills = skills ?? [];
		Monster = setup.Monster;
		ShowDuration = setup.ShowDuration + upgrades.ExtraShowTime;
		// Set up front so the full time can be shown before the clock starts (e.g. during a countdown).
		TimeLeft = rules.StageDuration + upgrades.ExtraStageTime;
		_freeMistakesLeft = upgrades.FreeMistakesPerStage;
	}

	public event Action<BattlePhase>? PhaseChanged;
	public event Action<AnswerResult>? AnswerChecked;

	/// <summary>Raised when a skill is used, with the damage it dealt (only Strike deals damage).</summary>
	public event Action<Skill, int>? SkillUsed;

	public Monster Monster { get; }

	/// <summary>How long each number is shown in this stage.</summary>
	public float ShowDuration { get; }

	/// <summary>How long the number currently on screen is shown: shorter while replaying it.</summary>
	public float CurrentShowDuration => _replaying ? SkillCatalog.ReplaySeconds : ShowDuration;

	public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
	public bool IsOver => Phase is BattlePhase.Won or BattlePhase.Lost;
	public float TimeLeft { get; private set; }
	public float PhaseElapsed { get; private set; }
	public string CurrentNumber { get; private set; } = "";
	public string Entered { get; private set; } = "";

	/// <summary>Seconds left on Time Stop; the stage clock doesn't run while this is above 0.</summary>
	public float TimeStopLeft { get; private set; }
	public bool IsTimeStopped => TimeStopLeft > 0f;
	public bool IsDoubleStrikeReady => _doubleStrikeReady;

	/// <summary>Digit count from the difficulty tracker, before upgrades.</summary>
	public int Digits => _difficulty.Digits;

	/// <summary>How long the numbers actually are, including extra digits from upgrades.</summary>
	public int NumberLength => Digits + _upgrades.ExtraDigits;

	public bool IsAtMaxDigits => Digits >= _rules.MaxDigits;
	public int CorrectStreak => _difficulty.CorrectStreak;
	public int CorrectStreakNeeded => _difficulty.CorrectStreakNeeded;

	/// <summary>Skills equipped for this run. Each can be used once per battle.</summary>
	public IReadOnlyList<Skill> Skills { get; }

	public void Start()
	{
		if (Phase != BattlePhase.NotStarted)
			throw new InvalidOperationException("A battle can only be started once.");
		NextRound();
	}

	public void Tick(float delta)
	{
		if (Phase == BattlePhase.NotStarted || IsOver)
			return;

		// Time Stop absorbs clock time first; the rounds themselves keep going.
		float frozen = Math.Min(TimeStopLeft, delta);
		TimeStopLeft -= frozen;
		TimeLeft = Math.Max(0f, TimeLeft - (delta - frozen));
		PhaseElapsed += delta;

		if (TimeLeft <= 0f)
			SetPhase(BattlePhase.Lost);
		else if (Phase == BattlePhase.Showing && PhaseElapsed >= CurrentShowDuration)
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

	public bool HasUsed(Skill skill) => _usedSkills.Contains(skill);

	public bool CanUseSkill(Skill skill) =>
		Skills.Contains(skill)
		&& !HasUsed(skill)
		&& Phase is BattlePhase.Showing or BattlePhase.Input or BattlePhase.Feedback
		&& (!skill.NeedsInput || Phase == BattlePhase.Input);

	/// <summary>Uses an equipped skill. Returns false (and does nothing) if it can't be used right now.</summary>
	public bool UseSkill(Skill skill)
	{
		if (!CanUseSkill(skill))
			return false;
		_usedSkills.Add(skill);

		int damage = 0;
		if (skill == SkillCatalog.Strike)
		{
			damage = (int)MathF.Ceiling(Monster.MaxHealth * SkillCatalog.StrikeHealthFraction);
			Monster.TakeDamage(damage);
			_stats.TotalDamage += damage;
		}
		else if (skill == SkillCatalog.TimeStop)
		{
			TimeStopLeft += SkillCatalog.TimeStopSeconds;
		}
		else if (skill == SkillCatalog.DoubleStrike)
		{
			_doubleStrikeReady = true;
		}
		else if (skill == SkillCatalog.SecondWind)
		{
			TimeLeft += SkillCatalog.SecondWindSeconds;
		}

		SkillUsed?.Invoke(skill, damage);

		// Skills that change the round go last, after the UI has heard about the skill.
		if (Monster.IsDefeated)
		{
			_stats.StagesCleared++;
			SetPhase(BattlePhase.Won);
		}
		else if (skill == SkillCatalog.Replay)
		{
			_replaying = true;
			SetPhase(BattlePhase.Showing); // Keeps what was entered so far.
		}
		else if (skill == SkillCatalog.Skip)
		{
			NextRound();
		}
		return true;
	}

	private void BeginInput()
	{
		if (_replaying)
			_replaying = false; // Carry on from what was entered before the replay.
		else
			Entered = _upgrades.RevealsFirstDigit ? CurrentNumber[..1] : ""; // The Hint upgrade fills in the first digit.
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
			if (_doubleStrikeReady)
			{
				damageMultiplier *= SkillCatalog.DoubleStrikeMultiplier;
				_doubleStrikeReady = false;
			}
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
		_replaying = false;
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
