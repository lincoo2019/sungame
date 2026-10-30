using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffFrozen : TowerDefenseCharacterBuffConfig
{
	public const string VisualKey = "Frozen";

	private static readonly Color ICE_SPEED_DOWN_COLOR = new Color(0.2f, 0.35f, 1f);

	[Export(PropertyHint.None, "")]
	public double time = 8.0;

	[Export(PropertyHint.None, "")]
	public double iceSpeedDownTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => ICE_SPEED_DOWN_COLOR;

	public override void _Init()
	{
		key = "Frozen";
	}

	public override void Enter()
	{
		if (character.instance.maskFlags == 0 || (character.instance.maskFlags & 2) != 0)
		{
			RejectFrozenAndApplyIceSpeedDown();
			return;
		}
		if ((character.instance.unUseBuffFlags & 2) != 0)
		{
			RejectFrozenAndApplyIceSpeedDown();
			return;
		}
		character.buff.DeleteBuff("Burn");
		ShowIceTrapVisual();
	}

	public override void EnterReadOnlyClient()
	{
		ShowIceTrapVisual();
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
		character.buff?.SetBuffVisualVisible("Frozen", visible: false);
		character.CreateIceTrap();
		ApplyIceSpeedDown();
	}

	private void RejectFrozenAndApplyIceSpeedDown()
	{
		character.buff.DeleteBuff("Frozen");
		ApplyIceSpeedDown();
	}

	private void ApplyIceSpeedDown()
	{
		if (iceSpeedDownTime != 0.0)
		{
			TowerDefenseCharacterBuffIceSpeedDown towerDefenseCharacterBuffIceSpeedDown = new TowerDefenseCharacterBuffIceSpeedDown();
			towerDefenseCharacterBuffIceSpeedDown.time = iceSpeedDownTime;
			character.buff.AddBuff(towerDefenseCharacterBuffIceSpeedDown);
		}
	}

	public override void Remove()
	{
		ExitReadOnlyClient();
	}

	public override void ExitReadOnlyClient()
	{
		character.buff?.SetBuffVisualVisible("Frozen", visible: false);
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = config as TowerDefenseCharacterBuffFrozen;
		time = Mathf.Max(time, towerDefenseCharacterBuffFrozen.time + GD.RandRange(-0.2, 0.2));
		iceSpeedDownTime = towerDefenseCharacterBuffFrozen.iceSpeedDownTime;
		currentTime = 0.0;
	}

	private void ShowIceTrapVisual()
	{
		BuffComponent buffComponent = character?.buff;
		if (buffComponent == null || buffComponent.IsReleased)
		{
			return;
		}
		Sprite2D cachedBuffVisual = buffComponent.GetCachedBuffVisual("Frozen");
		if (!GodotObject.IsInstanceValid(cachedBuffVisual))
		{
			if (!buffComponent.TryGetVisualDefinition("Frozen", out var visualDefinition) || visualDefinition == null || !visualDefinition.enabled || !GodotObject.IsInstanceValid(character.spriteGroup))
			{
				return;
			}
			cachedBuffVisual = CreateIceTrapSprite(visualDefinition);
			if (!GodotObject.IsInstanceValid(cachedBuffVisual))
			{
				return;
			}
			ConfigureIceTrapSprite(cachedBuffVisual, visualDefinition);
			character.spriteGroup.AddChild(cachedBuffVisual, forceReadableName: false, Node.InternalMode.Disabled);
			buffComponent.RegisterBuffVisual("Frozen", cachedBuffVisual, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, visualDefinition.drawBand));
		}
		buffComponent.SetBuffVisualVisible("Frozen", visible: true);
	}

	private static Sprite2D CreateIceTrapSprite(BuffVisualDefinition definition)
	{
		if (GodotObject.IsInstanceValid(definition.scene))
		{
			Node node = definition.scene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node is Sprite2D result)
			{
				return result;
			}
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
		return new Sprite2D();
	}

	private static void ConfigureIceTrapSprite(Sprite2D sprite, BuffVisualDefinition definition)
	{
		if (GodotObject.IsInstanceValid(definition.texture))
		{
			sprite.Texture = definition.texture;
		}
		sprite.Name = (definition.nodeName.IsEmpty ? new StringName("IcetrapSprite") : definition.nodeName);
		sprite.Position = definition.position;
		sprite.Scale = definition.scale;
		sprite.Rotation = definition.rotation;
		sprite.ZIndex = definition.zIndex;
		sprite.ZAsRelative = definition.zAsRelative;
		sprite.Centered = definition.centered;
		sprite.Offset = definition.offset;
		sprite.Visible = false;
	}
}
