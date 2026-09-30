using System;

namespace MemoryGame.Core;

public sealed class Monster
{
	private static readonly string[] Names = ["Slime", "Goblin", "Skeleton", "Orc", "Wraith", "Golem", "Dragon"];

	public Monster(string name, int maxHealth)
	{
		Name = name;
		MaxHealth = maxHealth;
		Health = maxHealth;
	}

	public string Name { get; }

	/// <summary>Stable lowercase key for this kind of monster, e.g. for looking up its art.</summary>
	public string Id => Name.ToLowerInvariant();
	public int MaxHealth { get; }
	public int Health { get; private set; }
	public bool IsDefeated => Health <= 0;

	public static Monster ForStage(int stage, GameRules rules)
	{
		int health = (int)MathF.Round(rules.MonsterBaseHealth * MathF.Pow(rules.MonsterHealthGrowth, stage - 1));
		return new Monster(Names[(stage - 1) % Names.Length], health);
	}

	public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
}
