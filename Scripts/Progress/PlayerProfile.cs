using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MemoryGame.Core;

namespace MemoryGame.Progress;

/// <param name="UnlockedSkill">The skill this run unlocked, if any (at most one per run).</param>
/// <param name="IsNewBest">The run reached a further stage than ever before.</param>
public sealed record RunReward(Skill? UnlockedSkill, bool IsNewBest);

/// <summary>What carries over between runs: the furthest stage reached, unlocked skills and the skill loadout.</summary>
public sealed class PlayerProfile
{
	// Plain settable properties so the profile round-trips through JSON.
	public int BestStage { get; set; }
	public List<string> UnlockedSkillIds { get; set; } = [];
	public List<string> EquippedSkillIds { get; set; } = [];

	/// <summary>Unlocked skills, in unlock order. Ids that no longer exist are ignored.</summary>
	[JsonIgnore]
	public IReadOnlyList<Skill> UnlockedSkills => SkillCatalog.All.Where(IsUnlocked).ToList();

	/// <summary>Equipped skills that are still valid (unlocked, at most <see cref="SkillCatalog.MaxEquipped"/>).</summary>
	[JsonIgnore]
	public IReadOnlyList<Skill> EquippedSkills =>
		SkillCatalog.All.Where(s => EquippedSkillIds.Contains(s.Id) && IsUnlocked(s)).Take(SkillCatalog.MaxEquipped).ToList();

	/// <summary>The next skill to unlock, or null once all are unlocked.</summary>
	[JsonIgnore]
	public Skill? NextLockedSkill => SkillCatalog.All.Where(s => !IsUnlocked(s)).MinBy(s => s.UnlockAtStage);

	public bool IsUnlocked(Skill skill) => UnlockedSkillIds.Contains(skill.Id);

	public void Equip(IReadOnlyCollection<Skill> skills)
	{
		if (skills.Count > SkillCatalog.MaxEquipped)
			throw new ArgumentException($"At most {SkillCatalog.MaxEquipped} skills can be equipped.", nameof(skills));
		if (skills.FirstOrDefault(s => !IsUnlocked(s)) is { } locked)
			throw new ArgumentException($"{locked.Name} isn't unlocked yet.", nameof(skills));
		EquippedSkillIds = skills.Select(s => s.Id).ToList();
	}

	/// <summary>
	/// Records a finished run. Unlocks at most one skill: the lowest-requirement locked skill, and only if
	/// the run cleared enough stages for it. A run that clears many stages still unlocks just that one.
	/// A new skill is equipped automatically when there's a free slot.
	/// </summary>
	public RunReward RecordRun(int stageReached, int stagesCleared)
	{
		bool isNewBest = stageReached > BestStage;
		if (isNewBest)
			BestStage = stageReached;

		Skill? unlocked = null;
		if (NextLockedSkill is { } next && stagesCleared >= next.UnlockAtStage)
		{
			UnlockedSkillIds.Add(next.Id);
			unlocked = next;
			if (EquippedSkills.Count < SkillCatalog.MaxEquipped)
				EquippedSkillIds = [.. EquippedSkills.Select(s => s.Id), next.Id];
		}
		return new RunReward(unlocked, isNewBest);
	}
}
