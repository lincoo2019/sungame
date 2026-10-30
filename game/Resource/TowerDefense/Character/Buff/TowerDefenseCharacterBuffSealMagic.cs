using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffSealMagic : TowerDefenseCharacterBuffConfig
{
	public const string Key = "SealMagic";

	private static PackedScene _SEAL_BUBBLE;

	[Export(PropertyHint.None, "")]
	public double time = 3.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public AdobeAnimateSprite sealSprite;

	private static PackedScene SEAL_BUBBLE => _SEAL_BUBBLE ?? (_SEAL_BUBBLE = GD.Load<PackedScene>("res://Asset/Anime/Effect/MagicBubbleS/MagicBubbleS.tscn"));

	public override void _Init()
	{
		key = "SealMagic";
	}

	public override void Enter()
	{
		if (!CanCarrySeal(character))
		{
			character.buff.DeleteBuff("SealMagic");
		}
		else
		{
			ShowSealVisual();
		}
	}

	public static bool CanCarrySeal(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character?.instance))
		{
			return false;
		}
		int maskFlags = character.instance.maskFlags;
		if (maskFlags != 0)
		{
			return (maskFlags & 2) == 0;
		}
		return false;
	}

	public override void EnterReadOnlyClient()
	{
		ShowSealVisual();
	}

	private void ShowSealVisual()
	{
		if (GodotObject.IsInstanceValid(sealSprite))
		{
			return;
		}
		PackedScene sEAL_BUBBLE = SEAL_BUBBLE;
		if (sEAL_BUBBLE != null)
		{
			Transform2D logicalGlobalTransform = character.GetLogicalGlobalTransform(character.spriteGroup);
			logicalGlobalTransform.Origin = character.GetLogicalGlobalPosition(character.sprite);
			AdobeAnimateSlot preferredSlot = null;
			if (GodotObject.IsInstanceValid(character.headSlot))
			{
				character.headSlot.Update();
				preferredSlot = character.headSlot;
			}
			sealSprite = sEAL_BUBBLE.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			character.AttachAnimatedStatusVisual(sealSprite, preferredSlot, logicalGlobalTransform);
			if (GodotObject.IsInstanceValid(character.sprite))
			{
				character.sprite.QueueRedraw();
			}
		}
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
		HideSealVisual();
	}

	public override void ExitReadOnlyClient()
	{
		HideSealVisual();
	}

	private void HideSealVisual()
	{
		if (GodotObject.IsInstanceValid(sealSprite))
		{
			character.DetachAnimatedStatusVisual(sealSprite);
			sealSprite.QueueFree();
		}
		sealSprite = null;
		character.buff?.ResumeAnimationIfNoPlaybackBlockingHardControl();
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffSealMagic towerDefenseCharacterBuffSealMagic = (TowerDefenseCharacterBuffSealMagic)config;
		time = Mathf.Max(time, towerDefenseCharacterBuffSealMagic.time);
		currentTime = 0.0;
	}
}
