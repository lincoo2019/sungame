using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffButterGene : TowerDefenseCharacterBuffConfig
{
	private static Texture2D _BUTTER_GENE_SPLAT;

	internal int freezeOrder;

	[Export(PropertyHint.None, "")]
	public double time = 4.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public Sprite2D butterSprite;

	private AdobeAnimateExternalVisualHandle _butterVisualHandle = AdobeAnimateExternalVisualHandle.Invalid;

	private bool _transformStarted;

	private bool _skipFreeze;

	private static Texture2D BUTTER_GENE_SPLAT => _BUTTER_GENE_SPLAT ?? (_BUTTER_GENE_SPLAT = GD.Load<Texture2D>("uid://dpkj242h0vnrg"));

	private bool IsFreezeOwner()
	{
		if (character?.buff == null)
		{
			return true;
		}
		if (character.buff.BuffGet("Butter") is TowerDefenseCharacterBuffButter towerDefenseCharacterBuffButter && towerDefenseCharacterBuffButter.freezeOrder > freezeOrder)
		{
			return false;
		}
		return true;
	}

	public override void _Init()
	{
		key = "ButterGene";
	}

	public override TowerDefenseCharacterBuffConfig CreateRuntimeInstance()
	{
		return new TowerDefenseCharacterBuffButterGene
		{
			key = key,
			refresh = refresh,
			canFliter = canFliter,
			time = time,
			currentTime = currentTime
		};
	}

	public override void Enter()
	{
		if (character.instance.maskFlags == 0)
		{
			character.buff.DeleteBuff("ButterGene");
			return;
		}
		if ((_skipFreeze = (character.instance.unUseBuffFlags & 4) != 0) && !CanTransform(character as TowerDefenseZombie))
		{
			character.buff.DeleteBuff("ButterGene");
			return;
		}
		freezeOrder = ++TowerDefenseCharacterBuffButter.NextFreezeOrder;
		if (CanTransform(character as TowerDefenseZombie))
		{
			if (!_transformStarted)
			{
				_transformStarted = true;
				Callable.From(StartGeneTransform).CallDeferred();
			}
		}
		else
		{
			ShowButterVisual();
		}
	}

	public override void EnterReadOnlyClient()
	{
		freezeOrder = ++TowerDefenseCharacterBuffButter.NextFreezeOrder;
		ShowButterVisual();
	}

	private void ShowButterVisual()
	{
		if (!GodotObject.IsInstanceValid(butterSprite))
		{
			butterSprite = new Sprite2D();
			butterSprite.Texture = BUTTER_GENE_SPLAT;
			butterSprite.Rotation = -0.4f;
			butterSprite.Scale = new Vector2(character.Scale.X * character.sprite.Scale.X, butterSprite.Scale.Y);
			character.spriteGroup.AddChild(butterSprite, forceReadableName: false, Node.InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(character.headSlot))
			{
				character.headSlot.Update();
				butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(character.headSlot) + new Vector2(-5f, -10f));
			}
			else if (character is TowerDefenseZombie towerDefenseZombie)
			{
				butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(towerDefenseZombie.sprite) + new Vector2(-25f, -30f));
			}
			else if (character is TowerDefensePlant towerDefensePlant)
			{
				butterSprite.Position = character.GetLogicalGlobalTransform(character.spriteGroup).AffineInverse() * (character.GetLogicalGlobalPosition(towerDefensePlant.sprite) + new Vector2(25f, -25f));
				butterSprite.Scale = new Vector2(butterSprite.Scale.X * -1f, butterSprite.Scale.Y);
			}
			_butterVisualHandle = character.RegisterCharacterExternalVisual(butterSprite, GodotObject.IsInstanceValid(character.headSlot) ? new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Slot, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation, character.headSlot, 1) : new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation));
		}
	}

	private void StartGeneTransform()
	{
		if (character is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie) && CanTransform(towerDefenseZombie))
		{
			bool skipFreeze = _skipFreeze;
			towerDefenseZombie.TransformTo("ZombieNormal", delegate(TowerDefenseCharacter normal)
			{
				ApplyButterFreeze(normal, skipFreeze);
			});
		}
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

	private static void ApplyButterFreeze(TowerDefenseCharacter normal, bool skipFreeze)
	{
		if (GodotObject.IsInstanceValid(normal) && normal.buff != null && !skipFreeze)
		{
			if (IsSpriteMediaReady(normal))
			{
				normal.buff.AddBuff(new TowerDefenseCharacterBuffButterGene
				{
					time = 4.0
				});
			}
			else
			{
				WaitMediaThenFreeze(normal, 0, skipFreeze);
			}
		}
	}

	private static bool IsSpriteMediaReady(TowerDefenseCharacter normal)
	{
		AdobeAnimateSprite sprite = normal.sprite;
		if (GodotObject.IsInstanceValid(sprite))
		{
			return sprite.GetLayerVisibleCountForRender() > 0;
		}
		return false;
	}

	private static void WaitMediaThenFreeze(TowerDefenseCharacter normal, int attempts, bool skipFreeze)
	{
		if (!GodotObject.IsInstanceValid(normal) || normal.buff == null || !normal.IsInsideTree())
		{
			return;
		}
		if (IsSpriteMediaReady(normal) || attempts >= 30)
		{
			if (!skipFreeze)
			{
				normal.buff.AddBuff(new TowerDefenseCharacterBuffButterGene
				{
					time = 4.0
				});
			}
		}
		else
		{
			normal.GetTree().CreateTimer(1.0 / 30.0).Timeout += delegate
			{
				WaitMediaThenFreeze(normal, attempts + 1, skipFreeze);
			};
		}
	}

	private static bool CanTransform(TowerDefenseZombie zombie)
	{
		if (zombie == null || !GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (zombie.config != null && zombie.config.name == "ZombieNormal" && zombie.GetArmor().Count == 0)
		{
			return false;
		}
		return true;
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
		time = Mathf.Max(time, ((TowerDefenseCharacterBuffButterGene)config).time);
		currentTime = 0.0;
	}
}
