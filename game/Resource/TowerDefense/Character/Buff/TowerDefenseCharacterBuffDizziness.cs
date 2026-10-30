using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffDizziness : TowerDefenseCharacterBuffConfig
{
	private static PackedScene _STAR;

	[Export(PropertyHint.None, "")]
	public double time = 3.0;

	[Export(PropertyHint.None, "")]
	public bool magicImmune;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public AdobeAnimateSprite dizzinessSprite;

	private static PackedScene STAR => _STAR ?? (_STAR = GD.Load<PackedScene>("uid://pvjsf1qinxav"));

	public override void _Init()
	{
		key = "Dizziness";
	}

	public override void Enter()
	{
		if (magicImmune && IsMagic())
		{
			character.buff.DeleteBuff("Dizziness");
		}
		else if (character.instance.maskFlags == 0 || (character.instance.maskFlags & 2) != 0)
		{
			character.buff.DeleteBuff("Dizziness");
		}
		else if ((character.instance.unUseBuffFlags & 0x20) != 0 || character.buff.BuffHas("RuneFogDizzyImmune"))
		{
			character.buff.DeleteBuff("Dizziness");
		}
		else
		{
			ShowDizzinessVisual();
		}
	}

	public override void EnterReadOnlyClient()
	{
		ShowDizzinessVisual();
	}

	private void ShowDizzinessVisual()
	{
		if (GodotObject.IsInstanceValid(character.headSlot) && dizzinessSprite == null)
		{
			character.headSlot.Update();
			dizzinessSprite = STAR.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			Transform2D logicalGlobalTransform = character.GetLogicalGlobalTransform(character.spriteGroup);
			logicalGlobalTransform.Origin = character.GetLogicalGlobalPosition(character.headSlot) + new Vector2(-5f, -10f);
			character.AttachAnimatedStatusVisual(dizzinessSprite, character.headSlot, logicalGlobalTransform);
		}
		character.sprite.QueueRedraw();
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (character.nearDie || character.die || currentTime >= time || (magicImmune && IsMagic()))
		{
			return true;
		}
		character.timeScale *= 0.0;
		return false;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		HideDizzinessVisual();
	}

	public override void ExitReadOnlyClient()
	{
		HideDizzinessVisual();
	}

	private void HideDizzinessVisual()
	{
		if (GodotObject.IsInstanceValid(dizzinessSprite))
		{
			character.DetachAnimatedStatusVisual(dizzinessSprite);
			dizzinessSprite.QueueFree();
		}
		dizzinessSprite = null;
		character.buff?.ResumeAnimationIfNoPlaybackBlockingHardControl();
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness = (TowerDefenseCharacterBuffDizziness)config;
		if (!towerDefenseCharacterBuffDizziness.magicImmune || !IsMagic())
		{
			time = Mathf.Max(time, towerDefenseCharacterBuffDizziness.time);
			magicImmune = magicImmune && towerDefenseCharacterBuffDizziness.magicImmune;
			currentTime = 0.0;
		}
	}

	private bool IsMagic()
	{
		if (!GodotObject.IsInstanceValid(character?.instance))
		{
			return false;
		}
		return (character.instance.physiqueTypeFlags & 0x1000) != 0;
	}
}
