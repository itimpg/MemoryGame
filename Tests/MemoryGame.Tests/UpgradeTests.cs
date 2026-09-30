using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;
using Xunit;
using static MemoryGame.Tests.BattleTests;

namespace MemoryGame.Tests;

public class UpgradeSetTests
{
	[Fact]
	public void Levels_stack_up_to_the_max()
	{
		var upgrades = new UpgradeSet();
		var power = UpgradeCatalog.Power;
		for (int i = 0; i < power.MaxLevel; i++)
			upgrades.Add(power);

		Assert.Equal(power.MaxLevel, upgrades.LevelOf(power));
		Assert.False(upgrades.CanTake(power));
		Assert.Throws<InvalidOperationException>(() => upgrades.Add(power));
	}

	[Fact]
	public void No_upgrades_means_no_bonus()
	{
		var upgrades = new UpgradeSet();
		Assert.Equal(1f, upgrades.DamageMultiplier(combo: 5));
		Assert.Equal(0f, upgrades.ExtraShowTime);
		Assert.Equal(0f, upgrades.ExtraStageTime);
		Assert.Equal(0, upgrades.ExtraDigits);
		Assert.False(upgrades.RevealsFirstDigit);
	}

	[Fact]
	public void Combo_bonus_counts_previous_correct_answers_up_to_the_cap()
	{
		var upgrades = new UpgradeSet();
		upgrades.Add(UpgradeCatalog.Combo);

		Assert.Equal(1f, upgrades.DamageMultiplier(combo: 1), 3);   // First answer: nothing before it.
		Assert.Equal(1.3f, upgrades.DamageMultiplier(combo: 4), 3); // Three before it.
		Assert.Equal(2f, upgrades.DamageMultiplier(combo: 50), 3);  // Capped at 10.
	}

	[Fact]
	public void Damage_bonuses_multiply()
	{
		var upgrades = new UpgradeSet();
		upgrades.Add(UpgradeCatalog.Power);
		upgrades.Add(UpgradeCatalog.Gamble);
		Assert.Equal(1.15f * 1.3f, upgrades.DamageMultiplier(combo: 1), 3);
	}

	[Fact]
	public void Owned_lists_upgrades_with_levels_in_catalog_order()
	{
		var upgrades = new UpgradeSet();
		upgrades.Add(UpgradeCatalog.Hint);
		upgrades.Add(UpgradeCatalog.Power);
		upgrades.Add(UpgradeCatalog.Power);

		Assert.Equal(
			[(UpgradeCatalog.Power, 2), (UpgradeCatalog.Hint, 1)],
			upgrades.Owned.ToList());
	}

	[Fact]
	public void Catalog_ids_are_unique_and_resolvable()
	{
		Assert.Equal(UpgradeCatalog.All.Count, UpgradeCatalog.All.Select(u => u.Id).Distinct().Count());
		foreach (var upgrade in UpgradeCatalog.All)
			Assert.Same(upgrade, UpgradeCatalog.ById(upgrade.Id));
	}
}

public class UpgradeOfferTests
{
	[Fact]
	public void Offers_three_different_upgrades()
	{
		var run = new Run(GameRules.Default, new Random(7));
		var offer = run.RollUpgradeOffer(UpgradeCatalog.All);
		Assert.Equal(3, offer.Count);
		Assert.Equal(3, offer.Distinct().Count());
	}

	[Fact]
	public void Never_offers_a_maxed_out_upgrade()
	{
		var run = new Run(GameRules.Default, new Random(7));
		run.Upgrades.Add(UpgradeCatalog.Hint); // Max level 1.
		for (int i = 0; i < 50; i++)
			Assert.DoesNotContain(UpgradeCatalog.Hint, run.RollUpgradeOffer(UpgradeCatalog.All));
	}

	[Fact]
	public void Offers_fewer_when_the_pool_runs_out()
	{
		var run = new Run(GameRules.Default, new Random(7));
		IReadOnlyList<Upgrade> pool = [UpgradeCatalog.Hint, UpgradeCatalog.Power];
		run.Upgrades.Add(UpgradeCatalog.Hint);

		Assert.Equal([UpgradeCatalog.Power], run.RollUpgradeOffer(pool));
	}
}

public class UpgradeEffectTests
{
	private static readonly GameRules Rules = GameRules.Default;

	private static UpgradeSet With(params Upgrade[] picks)
	{
		var upgrades = new UpgradeSet();
		foreach (var upgrade in picks)
			upgrades.Add(upgrade);
		return upgrades;
	}

	[Fact]
	public void Hint_fills_in_the_first_digit()
	{
		var battle = StartedBattle(upgrades: With(UpgradeCatalog.Hint));
		battle.Tick(battle.ShowDuration);

		Assert.Equal(BattlePhase.Input, battle.Phase);
		Assert.Equal(battle.CurrentNumber[..1], battle.Entered);
		Type(battle, battle.CurrentNumber[1..]);
		Assert.Equal(BattlePhase.Feedback, battle.Phase);
	}

	[Fact]
	public void Second_chance_forgives_one_mistake_per_level_per_stage()
	{
		var answers = new List<AnswerResult>();
		var battle = StartedBattle(answers: answers, upgrades: With(UpgradeCatalog.SecondChance));

		for (int round = 0; round < 2; round++)
		{
			battle.Tick(battle.ShowDuration);
			Type(battle, WrongAnswerFor(battle.CurrentNumber));
			battle.Tick(Rules.FeedbackDuration);
		}

		Assert.True(answers[0].MistakeForgiven);
		Assert.Equal(0f, answers[0].TimePenalty);
		Assert.False(answers[1].MistakeForgiven);
		Assert.Equal(Rules.WrongAnswerTimePenalty, answers[1].TimePenalty);
	}

	[Fact]
	public void Slow_time_and_extra_time_add_seconds()
	{
		var battle = StartedBattle(upgrades: With(UpgradeCatalog.SlowTime, UpgradeCatalog.TimeExtend));
		Assert.Equal(Rules.ShowDuration + UpgradeCatalog.SlowTimeSecondsPerLevel, battle.ShowDuration, 3);
		Assert.Equal(Rules.StageDuration + UpgradeCatalog.TimeExtendSecondsPerLevel, battle.TimeLeft, 3);
	}

	[Fact]
	public void Gamble_makes_numbers_longer_and_hit_harder()
	{
		var answers = new List<AnswerResult>();
		var battle = StartedBattle(answers: answers, upgrades: With(UpgradeCatalog.Gamble));
		Assert.Equal(Rules.StartingDigits + 1, battle.CurrentNumber.Length);

		battle.Tick(battle.ShowDuration);
		Type(battle, battle.CurrentNumber);

		int baseDamage = Damage.Calculate(Rules.StartingDigits + 1, 0f, Rules);
		Assert.Equal((int)MathF.Round(baseDamage * UpgradeCatalog.GambleDamageMultiplier), answers[0].Damage);
	}

	[Fact]
	public void Combo_grows_with_correct_answers_and_resets_on_a_mistake()
	{
		var answers = new List<AnswerResult>();
		var battle = StartedBattle(answers: answers, upgrades: With(UpgradeCatalog.Combo));

		void Answer(bool correct)
		{
			battle.Tick(battle.ShowDuration);
			Type(battle, correct ? battle.CurrentNumber : WrongAnswerFor(battle.CurrentNumber));
			battle.Tick(Rules.FeedbackDuration);
		}

		Answer(true);
		Answer(true);
		Answer(false);
		Answer(true);

		Assert.Equal(1f, answers[0].DamageMultiplier, 3);
		Assert.Equal(1.1f, answers[1].DamageMultiplier, 3);
		Assert.Equal(1f, answers[3].DamageMultiplier, 3);
	}
}
