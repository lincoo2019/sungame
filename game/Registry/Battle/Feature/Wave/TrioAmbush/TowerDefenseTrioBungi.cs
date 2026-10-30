using Godot;

[GlobalClass]
public partial class TowerDefenseTrioBungi : TowerDefenseZombie
{
	public TrioAmbushMember Member;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.invincible = true;
			instance.unUseBuffFlags |= 2;
			targetRegistrationComponent.canCarry = false;
			AddToGroup("Bungi");
			gravityUse = false;
			isGround = false;
			base.sprite.SetAnimation("Raise", loop: false, 0.2);
		}
	}

	public override void Walk()
	{
	}

	public override void WalkReady()
	{
	}

	public override bool CanBlock()
	{
		if (GodotObject.IsInstanceValid(Member))
		{
			return Member.CanRepel;
		}
		return false;
	}

	public override void Block(TowerDefenseCharacter target)
	{
		Member?.Repel();
	}
}
