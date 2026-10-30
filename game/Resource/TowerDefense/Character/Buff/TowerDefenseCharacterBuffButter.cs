using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffButter : TowerDefenseCharacterBuffConfig
{
	private static Texture2D _BUTTER_SPLAT;

	internal static int NextFreezeOrder;

	internal int freezeOrder;

	[Export(PropertyHint.None, "")]
	public double time = 8.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public Texture2D splatTexture;

	public Sprite2D butterSprite;

	private AdobeAnimateExternalVisualHandle _butterVisualHandle = AdobeAnimateExternalVisualHandle.Invalid;

	private static Texture2D BUTTER_SPLAT => _BUTTER_SPLAT ?? (_BUTTER_SPLAT = GD.Load<Texture2D>("uid://cf2k7klghh2nl"));

	public override void _Init()
	{
		key = "Butter";
	}

	private bool IsFreezeOwner()
	{
		if (character?.buff == null)
		{
			return true;
		}
		if (character.buff.BuffGet("ButterGene") is TowerDefenseCharacterBuffButterGene towerDefenseCharacterBuffButterGene && towerDefenseCharacterBuffButterGene.freezeOrder > freezeOrder)
		{
			return false;
		}
		return true;
	}

	public override void Enter()
	{
		if (character.instance.maskFlags == 0 || (character.instance.maskFlags & 2) != 0)
		{
			character.buff.DeleteBuff("Butter");
			return;
		}
		if ((character.instance.unUseBuffFlags & 4) != 0)
		{
			character.buff.DeleteBuff("Butter");
			return;
		}
		freezeOrder = ++NextFreezeOrder;
		ShowButterVisual();
	}

	public override void EnterReadOnlyClient()
	{
		freezeOrder = ++NextFreezeOrder;
		ShowButterVisual();
	}

	private void ShowButterVisual()
	{
		if (GodotObject.IsInstanceValid(butterSprite))
		{
			return;
		}
		butterSprite = new Sprite2D();
		butterSprite.Texture = ((splatTexture != null) ? splatTexture : BUTTER_SPLAT);
		butterSprite.Rotation = -0.4f;
		butterSprite.Scale = new Vector2(character.Scale.X * character.sprite.Scale.X, butterSprite.Scale.Y);
		character.spriteGroup.AddChild(butterSprite, forceReadableName: false, Node.InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(character.headSlot))
		{
			character.headSlot.Update();
			butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(character.headSlot) + new Vector2(-5f, -10f));
		}
		else
		{
			if (character is TowerDefenseZombie towerDefenseZombie)
			{
				butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(towerDefenseZombie.sprite) + new Vector2(-25f, -30f));
			}
			if (character is TowerDefensePlant towerDefensePlant)
			{
				butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(towerDefensePlant.sprite) + new Vector2(25f, -25f));
				butterSprite.Scale = new Vector2(butterSprite.Scale.X * -1f, butterSprite.Scale.Y);
			}
		}
		_butterVisualHandle = character.RegisterCharacterExternalVisual(butterSprite, GodotObject.IsInstanceValid(character.headSlot) ? new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Slot, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation, character.headSlot, 1) : new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (character.nearDie || character.die || currentTime >= time)
		{
			return true;
		}
		if (IsFreezeOwner())
		{
			character.timeScale *= 0.0;
		}
		return false;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
		HideButterVisual();
	}

	public override void ExitReadOnlyClient()
	{
		HideButterVisual();
	}

	private void HideButterVisual()
	{
		if (GodotObject.IsInstanceValid(butterSprite))
		{
			character.UnregisterCharacterExternalVisual(_butterVisualHandle);
			_butterVisualHandle = AdobeAnimateExternalVisualHandle.Invalid;
			butterSprite.QueueFree();
			butterSprite = null;
		}
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		time = Mathf.Max(time, ((TowerDefenseCharacterBuffButter)config).time);
		currentTime = 0.0;
		TowerDefenseCharacterBuffButter towerDefenseCharacterBuffButter = (TowerDefenseCharacterBuffButter)config;
		if (towerDefenseCharacterBuffButter.splatTexture != null)
		{
			splatTexture = towerDefenseCharacterBuffButter.splatTexture;
		}
	}
}
