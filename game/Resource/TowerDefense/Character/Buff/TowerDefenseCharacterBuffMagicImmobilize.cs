using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffMagicImmobilize : TowerDefenseCharacterBuffConfig
{
	private static PackedScene _STAR;

	[Export(PropertyHint.None, "")]
	public double time = 5.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public AdobeAnimateSprite immobilizeSprite;

	private static PackedScene STAR => _STAR ?? (_STAR = GD.Load<PackedScene>("uid://pvjsf1qinxav"));

	public override void _Init()
	{
		key = "MagicImmobilize";
	}

	public override void Enter()
	{
		if (IsImmobilizeImmune())
		{
			character.buff.DeleteBuff("MagicImmobilize");
		}
		else if (character.instance.maskFlags == 0 || (character.instance.maskFlags & 2) != 0)
		{
			character.buff.DeleteBuff("MagicImmobilize");
		}
		else
		{
			ShowImmobilizeVisual();
		}
	}

	public override void EnterReadOnlyClient()
	{
		ShowImmobilizeVisual();
	}

	private void ShowImmobilizeVisual()
	{
		if (GodotObject.IsInstanceValid(character.headSlot) && immobilizeSprite == null)
		{
			character.headSlot.Update();
			immobilizeSprite = STAR.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			Transform2D logicalGlobalTransform = character.GetLogicalGlobalTransform(character.spriteGroup);
			logicalGlobalTransform.Origin = character.GetLogicalGlobalPosition(character.headSlot) + new Vector2(-5f, -10f);
			character.AttachAnimatedStatusVisual(immobilizeSprite, character.headSlot, logicalGlobalTransform);
		}
		character.sprite.QueueRedraw();
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (character.nearDie || character.die || currentTime >= time)
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
		HideImmobilizeVisual();
	}

	public override void ExitReadOnlyClient()
	{
		HideImmobilizeVisual();
	}

	private void HideImmobilizeVisual()
	{
		if (GodotObject.IsInstanceValid(immobilizeSprite))
		{
			character.DetachAnimatedStatusVisual(immobilizeSprite);
			immobilizeSprite.QueueFree();
		}
		immobilizeSprite = null;
		character.buff?.ResumeAnimationIfNoPlaybackBlockingHardControl();
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffMagicImmobilize towerDefenseCharacterBuffMagicImmobilize = (TowerDefenseCharacterBuffMagicImmobilize)config;
		if (!IsImmobilizeImmune())
		{
			time = Mathf.Max(time, towerDefenseCharacterBuffMagicImmobilize.time);
			currentTime = 0.0;
		}
	}

	private bool IsImmobilizeImmune()
	{
		if (!GodotObject.IsInstanceValid(character?.instance))
		{
			return false;
		}
		return (character.instance.unUseBuffFlags & 0x2000) != 0;
	}
}
