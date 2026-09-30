using System;
using System.Collections.Generic;
using System.Linq;
using MemoryGame.Core;
using MemoryGame.Progress;
using Xunit;
using static MemoryGame.Tests.BattleTests;

namespace MemoryGame.Tests;

public class SkillCatalogTests
{
	[Fact]
	public void Ids_are_unique_and_unlock_stages_increase()
	{
		Assert.Equal(SkillCatalog.All.Count, SkillCatalog.All.Select(s => s.Id).Distinct().Count());
		var stages = SkillCatalog.All.Select(s => s.UnlockAtStage).ToList();
		Assert.Equal(stages.OrderBy(s => s), stages);
		Assert.Equal(2, stages[0]); // First skill for clearing stage 2.
		foreach (var skill in SkillCatalog.All)
			Assert.Same(skill, SkillCatalog.ById(skill.Id));
	}
}

public class SkillBattleTests
{
	private static readonly GameRules Rules = GameRules.Default;

	private static Battle BattleWith(Skill skill, int monsterHealth = 10_000, List<Skill>? used = null)
	{
		var setup = new StageSetup(1, new Monster("Test", monsterHealth), Rules.StartingDigits, Rules.ShowDuration);
		var battle = new Battle(Rules, new Random(3), setup, new RunStats(), new UpgradeSet(), [skill]);
		if (used is not null)
			battle.SkillUsed += (s, _) => used.Add(s);
		battle.Start();
		return battle;
	}

	[Fact]
	public void Skills_are_usable_once_per_battle()
	{
		var used = new List<Skill>();
		var battle = BattleWith(SkillCatalog.SecondWind, used: used);

		Assert.True(battle.UseSkill(SkillCatalog.SecondWind));
		Assert.False(battle.UseSkill(SkillCatalog.SecondWind));
		Assert.True(battle.HasUsed(SkillCatalog.SecondWind));
		Assert.Single(used);
	}

	[Fact]
	public void Unequipped_skills_and_skills_before_the_battle_starts_cannot_be_used()
	{
		var setup = new StageSetup(1, new Monster("Test", 100), Rules.StartingDigits, Rules.ShowDuration);
		var battle = new Battle(Rules, new Random(3), setup, new RunStats(), new UpgradeSet(), [SkillCatalog.Strike]);

		Assert.False(battle.UseSkill(SkillCatalog.Strike)); // Not started (e.g. during the countdown).
		battle.Start();
		Assert.False(battle.UseSkill(SkillCatalog.TimeStop)); // Not equipped.
		Assert.True(battle.UseSkill(SkillCatalog.Strike));
	}

	[Fact]
	public void Strike_deals_a_share_of_max_health_and_can_win()
	{
		var battle = BattleWith(SkillCatalog.Strike, monsterHealth: 1000);
		battle.UseSkill(SkillCatalog.Strike);
		Assert.Equal(850, battle.Monster.Health);

		var finisher = BattleWith(SkillCatalog.Strike, monsterHealth: 1);
		finisher.UseSkill(SkillCatalog.Strike);
		Assert.Equal(BattlePhase.Won, finisher.Phase);
	}

	[Fact]
	public void Time_stop_freezes_the_clock_but_not_the_rounds()
	{
		var battle = BattleWith(SkillCatalog.TimeStop);
		float before = battle.TimeLeft;
		battle.UseSkill(SkillCatalog.TimeStop);

		battle.Tick(Rules.ShowDuration); // 2 of the 5 frozen seconds.
		Assert.Equal(before, battle.TimeLeft);
		Assert.Equal(BattlePhase.Input, battle.Phase);

		battle.Tick(4f); // 3 more frozen, then 1 real second.
		Assert.Equal(before - 1f, battle.TimeLeft, 3);
		Assert.False(battle.IsTimeStopped);
	}

	[Fact]
	public void Replay_shows_the_number_again_and_keeps_what_was_entered()
	{
		var battle = BattleWith(SkillCatalog.Replay);
		Assert.False(battle.CanUseSkill(SkillCatalog.Replay)); // Only while entering the number.

		battle.Tick(battle.ShowDuration);
		string number = battle.CurrentNumber;
		Type(battle, number[..1]);
		Assert.True(battle.UseSkill(SkillCatalog.Replay));

		Assert.Equal(BattlePhase.Showing, battle.Phase);
		Assert.Equal(SkillCatalog.ReplaySeconds, battle.CurrentShowDuration);
		battle.Tick(SkillCatalog.ReplaySeconds);

		Assert.Equal(BattlePhase.Input, battle.Phase);
		Assert.Equal(number, battle.CurrentNumber);
		Assert.Equal(number[..1], battle.Entered);
		Assert.Equal(battle.ShowDuration, battle.CurrentShowDuration); // Back to normal for the next number.
	}

	[Fact]
	public void Double_strike_doubles_the_next_correct_answer_only()
	{
		var answers = new List<AnswerResult>();
		var battle = BattleWith(SkillCatalog.DoubleStrike);
		battle.AnswerChecked += answers.Add;
		battle.UseSkill(SkillCatalog.DoubleStrike);

		for (int round = 0; round < 2; round++)
		{
			battle.Tick(battle.ShowDuration);
			Type(battle, battle.CurrentNumber);
			battle.Tick(Rules.FeedbackDuration);
		}
		Assert.Equal(2f, answers[0].DamageMultiplier);
		Assert.Equal(1f, answers[1].DamageMultiplier);
	}

	[Fact]
	public void Second_wind_adds_time()
	{
		var battle = BattleWith(SkillCatalog.SecondWind);
		float before = battle.TimeLeft;
		battle.UseSkill(SkillCatalog.SecondWind);
		Assert.Equal(before + SkillCatalog.SecondWindSeconds, battle.TimeLeft);
	}

	[Fact]
	public void Skip_swaps_the_number_without_a_penalty()
	{
		var battle = BattleWith(SkillCatalog.Skip);
		battle.Tick(battle.ShowDuration);
		float before = battle.TimeLeft;
		battle.UseSkill(SkillCatalog.Skip);

		Assert.Equal(BattlePhase.Showing, battle.Phase);
		Assert.Equal("", battle.Entered);
		Assert.Equal(before, battle.TimeLeft);
	}
}

public class PlayerProfileTests
{
	[Fact]
	public void Clearing_only_one_stage_unlocks_nothing()
	{
		var profile = new PlayerProfile();
		var reward = profile.RecordRun(stageReached: 2, stagesCleared: 1);
		Assert.Null(reward.UnlockedSkill);
		Assert.Empty(profile.UnlockedSkills);
	}

	[Fact]
	public void Clearing_stage_2_unlocks_the_first_skill()
	{
		var profile = new PlayerProfile();
		var reward = profile.RecordRun(stageReached: 3, stagesCleared: 2);
		Assert.Equal(SkillCatalog.Strike, reward.UnlockedSkill);
	}

	[Fact]
	public void A_long_run_still_unlocks_only_the_lowest_skill()
	{
		var profile = new PlayerProfile();
		var reward = profile.RecordRun(stageReached: 7, stagesCleared: 6);
		Assert.Equal(SkillCatalog.Strike, reward.UnlockedSkill);
		Assert.Single(profile.UnlockedSkills);
	}

	[Fact]
	public void Each_skill_unlocks_the_first_time_its_stage_is_cleared()
	{
		var profile = new PlayerProfile();
		profile.RecordRun(3, 2); // Strike.

		Assert.Null(profile.RecordRun(3, 2).UnlockedSkill); // Stage 2 again: nothing new.
		Assert.Equal(SkillCatalog.Replay, profile.RecordRun(4, 3).UnlockedSkill);
		Assert.Equal(SkillCatalog.TimeStop, profile.RecordRun(9, 8).UnlockedSkill);
	}

	[Fact]
	public void Nothing_left_to_unlock_after_all_skills()
	{
		var profile = new PlayerProfile();
		for (int i = 0; i < SkillCatalog.All.Count; i++)
			profile.RecordRun(20, 19);

		Assert.Equal(SkillCatalog.All.Count, profile.UnlockedSkills.Count);
		Assert.Null(profile.NextLockedSkill);
		Assert.Null(profile.RecordRun(20, 19).UnlockedSkill);
	}

	[Fact]
	public void New_skills_fill_free_slots_but_not_a_full_loadout()
	{
		var profile = new PlayerProfile();
		profile.RecordRun(3, 2);
		profile.RecordRun(4, 3);
		profile.RecordRun(5, 4);

		Assert.Equal([SkillCatalog.Strike, SkillCatalog.Replay], profile.EquippedSkills);
		Assert.Equal(3, profile.UnlockedSkills.Count);
	}

	[Fact]
	public void Equip_accepts_up_to_two_unlocked_skills()
	{
		var profile = new PlayerProfile();
		profile.RecordRun(3, 2);
		profile.RecordRun(4, 3);
		profile.RecordRun(5, 4);

		profile.Equip([SkillCatalog.TimeStop]);
		Assert.Equal([SkillCatalog.TimeStop], profile.EquippedSkills);
		Assert.Throws<ArgumentException>(() => profile.Equip([SkillCatalog.Strike, SkillCatalog.Replay, SkillCatalog.TimeStop]));
		Assert.Throws<ArgumentException>(() => profile.Equip([SkillCatalog.Skip]));
	}

	[Fact]
	public void Best_stage_only_goes_up()
	{
		var profile = new PlayerProfile();
		Assert.True(profile.RecordRun(4, 3).IsNewBest);
		Assert.False(profile.RecordRun(2, 1).IsNewBest);
		Assert.Equal(4, profile.BestStage);
	}
}
