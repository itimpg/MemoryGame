using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;
using Xunit;

namespace MemoryGame.Tests;

public class DamageTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Theory]
	[InlineData(3, 0f, 60)]   // Instant: full bonus, 3 × (10 + 10).
	[InlineData(3, 2.5f, 45)] // Half bonus.
	[InlineData(3, 5f, 30)]   // Bonus gone.
	[InlineData(3, 30f, 30)]  // Never below the per-digit base.
	[InlineData(9, 0f, 180)]
	public void Damage_scales_with_digits_and_speed(int digits, float answerTime, int expected) =>
		Assert.Equal(expected, Damage.Calculate(digits, answerTime, Rules));
}

public class DifficultyTrackerTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Fact]
	public void Two_correct_in_a_row_adds_a_digit()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		Assert.Equal(0, tracker.RecordAnswer(true));
		Assert.Equal(+1, tracker.RecordAnswer(true));
		Assert.Equal(Rules.MinDigits + 1, tracker.Digits);
	}

	[Fact]
	public void Two_wrong_in_a_row_removes_a_digit()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		tracker.RecordAnswer(true);
		tracker.RecordAnswer(true);
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(-1, tracker.RecordAnswer(false));
		Assert.Equal(Rules.MinDigits, tracker.Digits);
	}

	[Fact]
	public void Alternating_answers_never_change_digits()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		for (int i = 0; i < 10; i++)
			Assert.Equal(0, tracker.RecordAnswer(i % 2 == 0));
	}

	[Fact]
	public void Each_digit_gained_doubles_the_streak_needed()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		foreach (int needed in new[] { 2, 4, 8, 16 })
		{
			Assert.Equal(needed, tracker.CorrectStreakNeeded);
			for (int i = 1; i < needed; i++)
				Assert.Equal(0, tracker.RecordAnswer(true));
			Assert.Equal(+1, tracker.RecordAnswer(true));
		}
		Assert.Equal(Rules.MinDigits + 4, tracker.Digits);
	}

	[Fact]
	public void Streak_needed_is_counted_from_the_stage_starting_digits()
	{
		var tracker = new DifficultyTracker(Rules, startingDigits: 6);
		Assert.Equal(Rules.CorrectStreakToAddDigit, tracker.CorrectStreakNeeded);
	}

	[Fact]
	public void Losing_a_digit_lowers_the_streak_needed_again()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		tracker.RecordAnswer(true);
		tracker.RecordAnswer(true);
		Assert.Equal(4, tracker.CorrectStreakNeeded);

		tracker.RecordAnswer(false);
		tracker.RecordAnswer(false);
		Assert.Equal(2, tracker.CorrectStreakNeeded);
	}

	[Fact]
	public void Wrong_answer_resets_the_correct_streak()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		tracker.RecordAnswer(true);
		tracker.RecordAnswer(false);
		Assert.Equal(0, tracker.RecordAnswer(true));
		Assert.Equal(+1, tracker.RecordAnswer(true));
	}

	[Fact]
	public void Digits_stay_within_limits()
	{
		var tracker = new DifficultyTracker(Rules, Rules.MinDigits);
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(0, tracker.RecordAnswer(false));
		Assert.Equal(Rules.MinDigits, tracker.Digits);

		for (int i = 0; i < 1000; i++)
			tracker.RecordAnswer(true);
		Assert.Equal(Rules.MaxDigits, tracker.Digits);
	}
}

public class MonsterTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Fact]
	public void Health_grows_each_stage()
	{
		Assert.Equal(240, Monster.ForStage(1, Rules).MaxHealth);
		Assert.Equal(312, Monster.ForStage(2, Rules).MaxHealth);
		Assert.Equal(406, Monster.ForStage(3, Rules).MaxHealth);
	}

	[Fact]
	public void Health_never_goes_below_zero()
	{
		var monster = new Monster("Test", 50);
		monster.TakeDamage(30);
		Assert.Equal(20, monster.Health);
		Assert.False(monster.IsDefeated);
		monster.TakeDamage(100);
		Assert.Equal(0, monster.Health);
		Assert.True(monster.IsDefeated);
	}
}

public class BattleTests
{
	private static readonly GameRules Rules = GameRules.Default;

	internal static Battle StartedBattle(
		int monsterHealth = 10_000,
		GameRules? rules = null,
		RunStats? stats = null,
		List<BattlePhase>? phases = null,
		List<AnswerResult>? answers = null,
		UpgradeSet? upgrades = null)
	{
		rules ??= Rules;
		var setup = new StageSetup(1, new Monster("Test", monsterHealth), rules.StartingDigits, rules.ShowDuration);
		var battle = new Battle(rules, new Random(42), setup, stats ?? new RunStats(), upgrades ?? new UpgradeSet());
		if (phases is not null)
			battle.PhaseChanged += phases.Add;
		if (answers is not null)
			battle.AnswerChecked += answers.Add;
		battle.Start();
		return battle;
	}

	internal static void Type(Battle battle, string digits)
	{
		foreach (char c in digits)
			battle.EnterDigit(c - '0');
	}

	internal static string WrongAnswerFor(string number) =>
		new(number.Select(c => c == '9' ? '0' : (char)(c + 1)).ToArray());

	private static void AnswerCorrectly(Battle battle, float answerTime = 0f)
	{
		battle.Tick(Rules.ShowDuration);
		if (answerTime > 0f)
			battle.Tick(answerTime);
		Type(battle, battle.CurrentNumber);
	}

	[Fact]
	public void Starts_by_showing_a_number_without_a_leading_zero()
	{
		var battle = StartedBattle();
		Assert.Equal(BattlePhase.Showing, battle.Phase);
		Assert.Equal(Rules.StartingDigits, battle.CurrentNumber.Length);
		Assert.NotEqual('0', battle.CurrentNumber[0]);
		Assert.True(battle.CurrentNumber.All(char.IsAsciiDigit));
	}

	[Fact]
	public void Hides_the_number_after_the_show_duration()
	{
		var battle = StartedBattle();
		battle.Tick(Rules.ShowDuration - 0.01f);
		Assert.Equal(BattlePhase.Showing, battle.Phase);
		battle.Tick(0.02f);
		Assert.Equal(BattlePhase.Input, battle.Phase);
	}

	[Fact]
	public void Ignores_digits_while_the_number_is_showing()
	{
		var battle = StartedBattle();
		battle.EnterDigit(1);
		Assert.Equal("", battle.Entered);
	}

	[Fact]
	public void Correct_answer_damages_the_monster()
	{
		var answers = new List<AnswerResult>();
		var stats = new RunStats();
		var battle = StartedBattle(stats: stats, answers: answers);
		battle.Tick(Rules.ShowDuration);
		battle.Tick(1f);

		string number = battle.CurrentNumber;
		Type(battle, number[..^1]);
		Assert.Empty(answers);
		Type(battle, number[^1..]);

		var answer = Assert.Single(answers);
		int expected = Damage.Calculate(number.Length, 1f, Rules);
		Assert.True(answer.IsCorrect);
		Assert.Equal(expected, answer.Damage);
		Assert.Equal(10_000 - expected, battle.Monster.Health);
		Assert.Equal(expected, stats.TotalDamage);
		Assert.Equal(BattlePhase.Feedback, battle.Phase);
	}

	[Fact]
	public void Wrong_answer_costs_time_and_deals_no_damage()
	{
		var answers = new List<AnswerResult>();
		var battle = StartedBattle(answers: answers);
		battle.Tick(Rules.ShowDuration);
		float before = battle.TimeLeft;
		Type(battle, WrongAnswerFor(battle.CurrentNumber));

		var answer = Assert.Single(answers);
		Assert.False(answer.IsCorrect);
		Assert.Equal(Rules.WrongAnswerTimePenalty, answer.TimePenalty);
		Assert.Equal(before - Rules.WrongAnswerTimePenalty, battle.TimeLeft, 3);
		Assert.Equal(10_000, battle.Monster.Health);
	}

	[Fact]
	public void Wrong_answer_with_less_time_than_the_penalty_loses()
	{
		var rules = Rules with { StageDuration = Rules.ShowDuration + 1f };
		var battle = StartedBattle(rules: rules);
		battle.Tick(rules.ShowDuration);
		Type(battle, WrongAnswerFor(battle.CurrentNumber));

		Assert.Equal(BattlePhase.Lost, battle.Phase);
		Assert.Equal(0f, battle.TimeLeft);
	}

	[Fact]
	public void Defeating_the_monster_wins_immediately()
	{
		var stats = new RunStats();
		var battle = StartedBattle(monsterHealth: 1, stats: stats);
		AnswerCorrectly(battle);

		Assert.Equal(BattlePhase.Won, battle.Phase);
		Assert.True(battle.Monster.IsDefeated);
		Assert.Equal(1, stats.StagesCleared);

		battle.Tick(Rules.StageDuration); // The clock stops once the battle is over.
		Assert.Equal(BattlePhase.Won, battle.Phase);
	}

	[Fact]
	public void Two_correct_answers_make_the_next_number_longer()
	{
		var battle = StartedBattle();
		for (int round = 0; round < 2; round++)
		{
			AnswerCorrectly(battle);
			battle.Tick(Rules.FeedbackDuration);
		}
		Assert.Equal(BattlePhase.Showing, battle.Phase);
		Assert.Equal(Rules.StartingDigits + 1, battle.CurrentNumber.Length);
	}

	[Fact]
	public void Loses_when_time_runs_out()
	{
		var phases = new List<BattlePhase>();
		var battle = StartedBattle(phases: phases);
		for (int i = 0; i < 1000 && !battle.IsOver; i++)
			battle.Tick(0.1f);

		Assert.Equal(BattlePhase.Lost, battle.Phase);
		Assert.Equal(BattlePhase.Lost, phases[^1]);
		Assert.Equal(0f, battle.TimeLeft);
	}
}
public class StageSetupTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Theory]
	[InlineData(1, 2, 2f)]
	[InlineData(2, 3, 2f)]
	[InlineData(6, 7, 2f)]      // First stage at the max digit count still gets the full show time.
	[InlineData(7, 7, 1.6f)]    // Then each stage shows it 0.8x as long...
	[InlineData(8, 7, 1.28f)]
	[InlineData(9, 7, 1.024f)]
	[InlineData(12, 7, 0.524f)]
	[InlineData(13, 7, 0.5f)]   // ...down to the minimum.
	[InlineData(30, 7, 0.5f)]
	public void Stages_add_digits_then_shrink_show_time_exponentially(int stage, int expectedDigits, float expectedShowDuration)
	{
		var setup = StageSetup.For(stage, Rules);
		Assert.Equal(expectedDigits, setup.StartingDigits);
		Assert.Equal(expectedShowDuration, setup.ShowDuration, 3);
	}
}

public class RunTests
{
	private static readonly GameRules Rules = GameRules.Default;

	[Fact]
	public void Each_stage_brings_a_tougher_monster()
	{
		var run = new Run(Rules, new Random(1));
		Assert.Equal(0, run.Stage);
		Assert.Equal(1, run.PeekNextStage().Stage);

		var first = run.StartNextStage();
		var second = run.StartNextStage();
		Assert.Equal(2, run.Stage);
		Assert.True(second.Monster.MaxHealth > first.Monster.MaxHealth);
	}

	[Fact]
	public void Each_stage_starts_with_its_own_digit_count()
	{
		var run = new Run(Rules with { MonsterBaseHealth = 1, MonsterHealthGrowth = 1f }, new Random(1));
		for (int stage = 1; stage <= 3; stage++)
		{
			var battle = run.StartNextStage();
			battle.Start();
			Assert.Equal(StageSetup.For(stage, Rules).StartingDigits, battle.CurrentNumber.Length);

			battle.Tick(battle.ShowDuration);
			foreach (char c in battle.CurrentNumber)
				battle.EnterDigit(c - '0');
			Assert.Equal(BattlePhase.Won, battle.Phase);
		}
		Assert.Equal(3, run.Stats.StagesCleared);
		Assert.Equal(3, run.Stats.Correct);
	}

	[Fact]
	public void Battle_uses_the_stage_show_duration()
	{
		var setup = StageSetup.For(12, Rules);
		var battle = new Battle(Rules, new Random(1), setup, new RunStats(), new UpgradeSet());
		battle.Start();
		battle.Tick(setup.ShowDuration - 0.01f);
		Assert.Equal(BattlePhase.Showing, battle.Phase);
		battle.Tick(0.02f);
		Assert.Equal(BattlePhase.Input, battle.Phase);
	}
}
