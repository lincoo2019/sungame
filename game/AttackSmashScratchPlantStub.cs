using System;
using Godot;

public partial class AttackSmashScratchPlantStub : TowerDefensePlant
{
	public int SmashHurtCalls;

	public int AttackDealCalls;

	public bool ThrowOnSmash;

	public Action OnAttackDeal;

	public override double SmashHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		SmashHurtCalls++;
		if (ThrowOnSmash)
		{
			throw new InvalidOperationException("intentional smash scratch exception");
		}
		return num;
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		AttackDealCalls++;
		OnAttackDeal?.Invoke();
	}
}
